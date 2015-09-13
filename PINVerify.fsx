
open System
open System.IO
open System.Globalization

let raw = File.ReadAllLines(@"c:\rbi\SomePINLog.log20150909")
let raw' = raw |> Array.filter (fun l -> l.Contains("VALIDATEPINPADPIN"))

// could regex 
(*
2015-09-09 08:04:41,692 DEBUG - VALIDATEPINPADPIN started. 123456******1234********************
2015-09-09 08:04:42,661 DEBUG - Status for card 123456******1234 is 00 Access Approved
2015-09-09 08:04:42,864 DEBUG - VALIDATEPINPADPIN finished. Status: successful 123456******1234
*)
// but in this case we reliabily know the positions

// the same card could have been used at a pin pad multiple times in a row, so have to iterate forward here looking for next
// matched finished after a start, this should reasonably occur within ... let's say the next 5 lines
let findAdjacentFinished (lines:string[]) card =
        lines |> Array.tryFind (fun line -> (line.Contains card) && line.Contains("finished"))

let pinVerifyDurations =
    raw'
    |> Array.mapi (fun i l -> 
                        // search ahead up to 5 lines, stop before getting to last 5 lines
                        if l.Contains("started") && i < raw'.Length-5 then
                            Some(
                                let start = DateTime.ParseExact(l.Substring(0,23), "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.CurrentCulture)
                                let card = l.Substring(60,15)

                                match findAdjacentFinished raw'.[i..i+5] card with
                                | Some line -> let finished = DateTime.ParseExact(line.Substring(0,23),  "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.CurrentCulture)
                                               Some(start, finished, finished - start)
                                | _ -> printfn "No match for line %i" i
                                       None
                            )
                        else
                            None
                        
                )
    |> Array.filter (fun i -> i.IsSome)
    |> Array.map (fun i -> Option.get i) // unwrap the option that a line may not have 'started' in it
    |> Array.filter (fun i -> i.IsSome)
    |> Array.map (fun i -> Option.get i) // unwrap the option a matching 'finished' may not have been found


// branched fsharp.charting to add the histogram https://github.com/bohdanszymanik/FSharp.Charting 
#I @"C:\Temp\FSharp.Charting-bohdanszymanik-column\FSharp.Charting-bohdanszymanik-column\src\bin\Debug"
#r "System.Windows.Forms.DataVisualization"

#r "FSharp.Charting.dll"

open FSharp.Charting
open System

module FsiAutoShow = 
    fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) -> ch.ShowChart() |> ignore; "(Chart)")

pinVerifyDurations |> Array.map( fun (_,_,d) -> (float)d.Milliseconds) |> Chart.Histogram
