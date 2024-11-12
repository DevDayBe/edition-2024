@echo off

setlocal

set DOTNET_GCName=clrgc.dll
set DOTNET_GCConserveMemory=9
set DOTNET_GCHeapCount=4
set DOTNET_GCSegmentSize=4000000

pushd %~dp0\DatasTest\bin\Debug\net8.0
%~dp0\DatasTest\bin\Debug\net8.0\DatasTest.exe gummycat
popd

