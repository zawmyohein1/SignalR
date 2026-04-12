using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using JobRealtimeSample.FrameworkApi.Controllers;
using JobRealtimeSample.FrameworkApi.Vendors;

namespace JobRealtimeSample.FrameworkApi
{
    public sealed class FrameworkApiDependencyResolver : IDependencyResolver
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(LeaveCalculationsVendor))
            {
                return LeaveCalculationCompositionRoot.Vendor;
            }

            if (serviceType == typeof(LeaveCalculationsController))
            {
                return new LeaveCalculationsController(LeaveCalculationCompositionRoot.Vendor);
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);

            return service is null
                ? Array.Empty<object>()
                : new[] { service };
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
