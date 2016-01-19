﻿#I "../../bin"
#r "FsPickler.dll"
#r "AWSSDK.Core.dll"
#r "AWSSDK.S3.dll"
#r "AWSSDK.DynamoDBv2.dll"
#r "AWSSDK.SQS.dll"
#r "MBrace.Core.dll"
#r "MBrace.Runtime.dll"
#r "MBrace.Aws.dll"


open Amazon
open Amazon.Runtime
open Amazon.S3
open Amazon.SQS
open Amazon.DynamoDBv2

open MBrace.Core
open MBrace.Core.Internals
open MBrace.Aws.Runtime
open MBrace.Aws.Store

let account = AwsAccount.Create("Default", RegionEndpoint.EUCentral1)
let store = S3FileStore.Create(account) :> ICloudFileStore

let run x = Async.RunSynchronously x

let clearBuckets() = async {
    let! buckets = store.EnumerateDirectories "/"
    let! _ = buckets |> Seq.filter (fun b -> b.StartsWith "/mbrace") |> Seq.map (fun b -> store.DeleteDirectory(b,true)) |> Async.Parallel
    return ()
}

clearBuckets() |> run

let dir = store.GetRandomDirectoryName()
store.DirectoryExists dir |> run
store.CreateDirectory dir |> run
store.DirectoryExists dir |> run

store.EnumerateDirectories store.RootDirectory |> run


let files = [for i in 1 .. 10 -> store.Combine(dir, sprintf "file-%d" i)]

files |> Seq.map (fun f -> async { use! s = store.BeginWrite f in s.WriteByte 1uy}) |> Async.Parallel |> run

account.S3Client.DeleteObject("mbrace34f3d50021d64666a9815b617410891f", "poutsa")

store.EnumerateFiles dir |> run