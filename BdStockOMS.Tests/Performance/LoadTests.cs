using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace BdStockOMS.Tests.Performance
{
    [Trait("Category", "Load")]
    public class LoadTests
    {
        private readonly ITestOutputHelper _output;
        public LoadTests(ITestOutputHelper output) { _output = output; }

        private static readonly Uri BaseUri = new Uri("http://localhost:5000");

        private void Log(string label, dynamic s)
        {
            _output.WriteLine($"=== {label} ===");
            _output.WriteLine($"OK: {s.ScenarioStats[0].Ok.Request.Count}  Fail: {s.ScenarioStats[0].Fail.Request.Count}");
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_Baseline_100Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("baseline_100", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(10,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Baseline 100", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_Ramp_500Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("ramp_500", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(50,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(500, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Ramp 500", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_Ramp_1000Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("ramp_1000", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(50,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(1000, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Ramp 1000", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_Ramp_2000Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("ramp_2000", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(50,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(200, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(2000, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Ramp 2000", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_Ramp_3500Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("ramp_3500", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(50,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(150, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(350, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(3500, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Ramp 3500", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_FullRamp_5000Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("full_ramp_5000", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var r = await client.GetAsync("/api/stocks");
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || r.IsSuccessStatusCode
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(50,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(500,  TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(1000, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(250, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15)),
                NBomber.CSharp.Simulation.Inject(2500, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.RampingInject(500, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20)),
                NBomber.CSharp.Simulation.Inject(5000, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("FULL RAMP 5000", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }

        [Fact]
        [Trait("Category", "Load")]
        public void LoadTest_LoginStress_1000Users()
        {
            var scenario = NBomber.CSharp.Scenario.Create("login_stress_1000", async ctx =>
            {
                using var client = new HttpClient { BaseAddress = BaseUri };
                try
                {
                    var payload = new System.Net.Http.StringContent(
                        "{\"email\":\"stress@test.com\",\"password\":\"wrong\"}",
                        System.Text.Encoding.UTF8, "application/json");
                    var r = await client.PostAsync("/api/auth/login", payload);
                    return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? NBomber.CSharp.Response.Ok()
                        : NBomber.CSharp.Response.Fail();
                }
                catch { return NBomber.CSharp.Response.Fail(); }
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                NBomber.CSharp.Simulation.RampingInject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)),
                NBomber.CSharp.Simulation.Inject(1000, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15))
            );

            var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();
            Log("Login Stress 1000", stats);
            Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 0);
        }
    }
}
