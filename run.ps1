$target = $( gum choose benchmark example )

function benchmark()
{
    Set-Location ./benchmark
    dotnet publish -c Release -o ./bin/Release/publish
    Set-Location ../
    ./benchmark/bin/Release/publish/benchmark.exe
}

function example()
{
    Set-Location ./example
    dotnet publish -c Release -o ./bin/Release/publish
    Set-Location ../
    ./example/bin/Release/publish/example.exe
}

switch ($target)
{
    "benchmark" {
        benchmark;
    }
    "example" {
        example;
    }
}