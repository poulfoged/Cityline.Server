using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cityline.Server
{
	public class CitylineHealthCheck : IHealthCheck
    {
        private readonly int _serverCountAlertLimit;

        public CitylineHealthCheck(int serverCountAlertLimit = 200)
		{
            _serverCountAlertLimit = serverCountAlertLimit;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var cityserverCount  = CitylineServer._instanceCount;
            IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
            {
                { "cityline-server-count", cityserverCount }
            };

            if (cityserverCount > _serverCountAlertLimit)
                return Task.FromResult(HealthCheckResult.Degraded("Cityline server instance coutn above limit", null, data));

            return Task.FromResult(HealthCheckResult.Healthy("All ok", data));
        }
    }
}

