

using BenchmarkDotNet.Running;


if (args.Contains("class-when-many") == true)
{
    Rf_ClassWhen_Many_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_ClassWhen_Many_Benchmark>();
}
if (args.Contains("class-when-one") == true)
{
    Rf_ClassWhen_One_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_ClassWhen_One_Benchmark>();
}