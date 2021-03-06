﻿namespace MBrace.AWS.Runtime

open System

open MBrace.Runtime
open MBrace.AWS.Runtime.Utilities

open FSharp.AWS.DynamoDB

[<AutoSerializable(true)>]
type WorkerId internal (workerId : string) = 
    member this.Id = workerId

    interface IWorkerId with
        member this.CompareTo(obj: obj): int =
            match obj with
            | :? WorkerId as w -> compare workerId w.Id
            | _ -> invalidArg "obj" "invalid comparand."
        
        member this.Id: string = this.Id

    override this.ToString() = this.Id
    override this.Equals(other:obj) =
        match other with
        | :? WorkerId as w -> workerId = w.Id
        | _ -> false

    override this.GetHashCode() = hash workerId

[<ConstantHashKey("HashKey", "Worker")>]
type WorkerRecord =
    {
        [<RangeKey; CustomName("RangeKey")>]
        WorkerId : string

        Hostname : string
        ProcessId : int
        ProcessName : string
        InitializationTime : DateTimeOffset
        LastHeartBeat : DateTimeOffset
        MaxWorkItems : int
        ActiveWorkItems : int
        ProcessorCount : int
        HeartbeatInterval : TimeSpan
        HeartbeatThreshold : TimeSpan
        Version : string
        PerformanceInfo : PerformanceInfo

        [<FsPicklerJson>]
        ExecutionStatus : WorkerExecutionStatus
    }

[<AutoOpen>]
module internal WorkerRecordUtils =
    
    let private template = template<WorkerRecord>

    let workerRecordKeyCondition = template.ConstantHashKeyCondition |> Option.get

    let updateExecutionStatus =
        <@ fun s (r:WorkerRecord) -> { r with ExecutionStatus = s } @>
        |> template.PrecomputeUpdateExpr

    let incrWorkItemCount =
        <@ fun (r:WorkerRecord) -> { r with ActiveWorkItems = r.ActiveWorkItems + 1 } @>
        |> template.PrecomputeUpdateExpr

    let decrWorkItemCount =
        <@ fun (r:WorkerRecord) -> { r with ActiveWorkItems = r.ActiveWorkItems - 1 } @>
        |> template.PrecomputeUpdateExpr

    let updatePerfMetrics =
        <@ fun p (r:WorkerRecord) -> { r with PerformanceInfo = p } @>
        |> template.PrecomputeUpdateExpr

    let updateLastHeartbeat =
        <@ fun hb (r:WorkerRecord) -> { r with LastHeartBeat = hb } @>
        |> template.PrecomputeUpdateExpr