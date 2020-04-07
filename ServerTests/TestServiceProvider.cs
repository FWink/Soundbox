using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    /// <summary>
    /// Implements a <see cref="IServiceProvider"/> for unit testing.
    /// Returned services may be fully functioning services from the Server projects, Mocks, or specialized test implementations (e.g. <see cref="IConfiguration"/> providing a test configuration).
    /// </summary>
    class TestServiceProvider : IServiceProvider
    {
        /// <inheritdoc/>
        /// <exception cref="ServiceException"/>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(IServiceProvider)))
                return this;
            //TODO
            if (serviceType.Equals(typeof(IHubContext<SoundboxHub, ISoundboxClient>)))
                return null;
            if (serviceType.Equals(typeof(IConfiguration)))
                return null;

            return BuildService(serviceType);
        }

        /// <summary>
        /// Constructs an object of the given type by attempting to call the next best constructor the type can offer.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        /// <exception cref="ServiceException" />
        protected object BuildService(Type serviceType)
        {
            if (!serviceType.IsClass)
                throw new BuildServiceException(serviceType);

            foreach(var constructor in serviceType.GetConstructors())
            {
                //try to call this one
                var parametersRequired = constructor.GetParameters();
                var parameters = new object[parametersRequired.Length];

                for(int i = 0; i < parametersRequired.Length; ++i)
                {
                    parameters[i] = GetService(parametersRequired[i].ParameterType);
                }

                return constructor.Invoke(parameters);
            }

            throw new BuildServiceException(serviceType);
        }

        public class ServiceException : Exception
        {
            public Type ServiceType { get; }

            public ServiceException(Type serviceType, string message) : this(serviceType, message, null) { }

            public ServiceException(Type serviceType, string message, Exception innerException) : base(message, innerException)
            {
                this.ServiceType = serviceType;
            }
        }

        public class BuildServiceException : ServiceException
        {
            public BuildServiceException(Type serviceType) : base(serviceType, $"Cannot construct service of type '{serviceType.FullName}'") { }
        }
    }
}
