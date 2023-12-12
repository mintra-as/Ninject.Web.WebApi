// -------------------------------------------------------------------------------------------------
// <copyright file="NinjectWebApiHttpApplicationPlugin.cs" company="Ninject Project Contributors">
//   Copyright (c) 2007-2010 Enkari, Ltd. All rights reserved.
//   Copyright (c) 2010-2017 Ninject Project Contributors. All rights reserved.
//
//   Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
//   You may not use this file except in compliance with one of the Licenses.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//   or
//       http://www.microsoft.com/opensource/licenses.mspx
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Ninject.Web.WebApi
{
    using System.Linq;
    using System.Runtime.Remoting.Messaging;
    using System.Web.Http;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Filters;
    using System.Web.Http.Validation;

    using Ninject.Activation;
    using Ninject.Components;
    using Ninject.Web.Common;
    using Ninject.Web.WebApi.Filter;

    /// <summary>
    /// The web plugin implementation for WebApi.
    /// </summary>
    public class NinjectWebApiHttpApplicationPlugin : NinjectComponent, INinjectHttpApplicationPlugin
    {
        /// <summary>
        /// The ninject kernel.
        /// </summary>
        private readonly IKernel kernel;

        /// <summary>
        /// Lock used to prevent multiple threads accessing <see cref="Start"/>.
        /// </summary>
        private readonly object initLock = new object();

        /// <summary>
        /// Used to store state of whether the plugin has been initialized.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectWebApiHttpApplicationPlugin"/> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public NinjectWebApiHttpApplicationPlugin(IKernel kernel)
        {
            this.kernel = kernel;
        }

        /// <summary>
        /// Gets the request scope.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The request scope.</returns>
        public object GetRequestScope(IContext context)
        {
            return CallContext.LogicalGetData(NinjectDependencyResolver.NinjectWebApiRequestScope);
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            lock (this.initLock)
            {
                var config = this.kernel.Get<HttpConfiguration>();

                // Prevent duplicate bindings when multiple bootstrappers are in use
                if (!this.initialized)
                {
                    var defaultFilterProviders =
                        config.Services.GetServices(typeof(IFilterProvider)).Cast<IFilterProvider>();
                    config.Services.Clear(typeof(IFilterProvider));
                    this.kernel.Bind<DefaultFilterProviders>()
                        .ToConstant(new DefaultFilterProviders(defaultFilterProviders));

                    var modelValidatorProviders = config.Services.GetServices(typeof(ModelValidatorProvider))
                        .Cast<ModelValidatorProvider>();
                    config.Services.Clear(typeof(ModelValidatorProvider));
                    this.kernel.Bind<DefaultModelValidatorProviders>()
                        .ToConstant(new DefaultModelValidatorProviders(modelValidatorProviders));
                }

                config.DependencyResolver = this.CreateDependencyResolver();

                this.initialized = true;
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// Creates the controller factory that is used to create the controllers.
        /// </summary>
        /// <returns>The created controller factory.</returns>
        protected IDependencyResolver CreateDependencyResolver()
        {
            return this.kernel.Get<IDependencyResolver>();
        }
    }
}