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
    class TestServiceProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// List of all services that we created. They get disposed in <see cref="Dispose"/>
        /// </summary>
        protected ICollection<object> Services = new List<object>();

        /// <inheritdoc/>
        /// <exception cref="ServiceException"/>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(IServiceProvider)))
                return this;
            if (serviceType.Equals(typeof(ISoundboxConfigProvider)))
                return BuildService(typeof(TestConfigProvider));
            //TODO
            if (serviceType.Equals(typeof(IHubContext<SoundboxHub, ISoundboxClient>)))
                return null;
            if (serviceType.Equals(typeof(IConfiguration)))
                return null;
            if (serviceType.Equals(typeof(IDatabaseProvider)))
                return null;
            if (typeof(Microsoft.Extensions.Logging.ILogger).IsAssignableFrom(serviceType))
            {
                if (serviceType.IsGenericType)
                {
                    //ILogger<T>
                    Type loggerType = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>);
                    Type genericLogerType = loggerType.MakeGenericType(serviceType.GetGenericArguments());
                    return genericLogerType.GetConstructor(Type.EmptyTypes).Invoke(null);
                }
                else
                {
                    return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                }
            }
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition().Equals(typeof(Microsoft.Extensions.Options.IOptions<>)))
            {
                //use a default value
                return typeof(TestOptions<>).MakeGenericType(serviceType.GetGenericArguments()).GetConstructor(Type.EmptyTypes).Invoke(null);
            }

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

                object service = constructor.Invoke(parameters);
                Services.Add(service);

                return service;
            }

            throw new BuildServiceException(serviceType);
        }

        public void Dispose()
        {
            foreach(var service in Services)
            {
                if(service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            Services.Clear();
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
