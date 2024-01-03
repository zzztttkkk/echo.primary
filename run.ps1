$target = $( gum choose benchmark )

function benchmark()
{
    Set-Location ./benchmark
    dotnet publish -c Release -o ./bin/Release/publish
    Set-Location ../
    ./benchmark/bin/Release/publish/benchmark.exe
}

switch ($target)
{
    "benchmark" {
        benchmark;
    }
}