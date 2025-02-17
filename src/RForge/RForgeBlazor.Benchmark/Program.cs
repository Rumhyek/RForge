﻿

using BenchmarkDotNet.Running;


if (args.Any(a => a == "rf-class-when-many") == true)
{
    Rf_ClassWhen_Many_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_ClassWhen_Many_Benchmark>();
}
if (args.Any(a => a == "rf-class-when-one") == true)
{
    Rf_ClassWhen_One_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_ClassWhen_One_Benchmark>();
}

if (args.Any(a => a == "rf-style-when-many") == true)
{
    Rf_StyleWhen_Many_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_StyleWhen_Many_Benchmark>();
}

if (args.Any(a => a == "rf-style-when-one") == true)
{
    Rf_StyleWhen_One_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_StyleWhen_One_Benchmark>();
}

if (args.Any(a => a == "rf-class-many") == true)
{
    Rf_Class_Many_Benchmark.PrintValues();
    BenchmarkRunner.Run<Rf_Class_Many_Benchmark>();
}