using System.Collections;
using Castle.Core;
using Castle.Core.Logging;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using NUnit.Framework;

namespace EventSystem
{

    #region For Castle MicroKernel

    internal static class ServiceConfigurator
    {
        static ServiceConfigurator()
        {
            AddComponent<RpcCalls>();
            AddComponent<SampleEventFactory>();
            AddComponent<AbstractEventFactory, SampleEventFactory>();
            // Add your components.
        }

        public static void Dispose()
        {
            container.Dispose();
        }

        #region Generics API.

        public static ServiceType Resolve<ServiceType>()
        {
            return container.Resolve<ServiceType>();
        }

        public static ServiceType Resolve<ServiceType>(IDictionary arguments)
        {
            return (ServiceType) container.Kernel.Resolve(typeof (ServiceType), arguments);
        }

        public static void RegisterCustomDependencies<ServiceType>(IDictionary dependencies)
        {
            container.Kernel.RegisterCustomDependencies(typeof (ServiceType), dependencies);
        }

        public static void AddComponent<ClassType>()
        {
            container.AddComponent(typeof (ClassType).FullName, typeof (ClassType));
        }

        public static void AddComponent<ServiceType, ClassType>()
        {
            container.AddComponent(typeof (ServiceType).FullName, typeof (ServiceType), typeof (ClassType));
        }

        public static void ReleaseComponent(object instance)
        {
            container.Release(instance);
        }

        #endregion

        public static IWindsorContainer Container
        {
            get { return container; }
        }

        public static ILoggerFactory LogFactory
        {
            get { return Resolve<ILoggerFactory>(); }
        }

        private static readonly IWindsorContainer container =
            new WindsorContainer(new XmlInterpreter("WindsorConfig.xml"));
    }

    #region Unit Tests

    namespace Tests
    {
        internal abstract class AbstractCounter
        {
            public AbstractCounter()
            {
                count = 0;
            }

            public void Increment()
            {
                count++;
            }

            public int Count
            {
                get { return count; }
            }

            protected int count;
        }

        [Singleton]
        internal class SingletonUsesAtAnyTime : AbstractCounter
        {
        }

        [Singleton]
        internal class ResettableSingleton : AbstractCounter
        {
            public void Reset()
            {
                count = 0;
            }
        }

        [Transient]
        internal class CountIncrementor
        {
            public CountIncrementor(SingletonUsesAtAnyTime singleton)
            {
                singleton.Increment();
            }
        }

        [TestFixture]
        public class MicroKernelSingletonTestCase
        {
            [SetUp]
            public void SetUp()
            {
                container = new WindsorContainer(new XmlInterpreter("WindsorConfig.xml"));
                container.AddComponent("atanytime", typeof (SingletonUsesAtAnyTime));
                container.AddComponent("resettable", typeof (ResettableSingleton));
                container.AddComponent("incrementor", typeof (CountIncrementor));
            }

            [TearDown]
            public void TearDown()
            {
                container.Dispose();
            }

            [Test]
            public void SameInstance()
            {
                SingletonUsesAtAnyTime first = container[typeof (SingletonUsesAtAnyTime)] as SingletonUsesAtAnyTime;
                SingletonUsesAtAnyTime second = container[typeof (SingletonUsesAtAnyTime)] as SingletonUsesAtAnyTime;
                Assert.AreSame(first, second);
            }

            [Test]
            public void HoldingCount()
            {
                SingletonUsesAtAnyTime first = container[typeof (SingletonUsesAtAnyTime)] as SingletonUsesAtAnyTime;
                Assert.AreEqual(first.Count, 0);
                first.Increment();
                Assert.AreEqual(first.Count, 1);

                SingletonUsesAtAnyTime second = container[typeof (SingletonUsesAtAnyTime)] as SingletonUsesAtAnyTime;
                Assert.AreEqual(second.Count, 1);
            }

            [Test]
            public void ResetCount()
            {
                ResettableSingleton first = container[typeof (ResettableSingleton)] as ResettableSingleton;
                first.Increment();
                Assert.AreEqual(first.Count, 1);

                ResettableSingleton second = container[typeof (ResettableSingleton)] as ResettableSingleton;
                Assert.AreEqual(second.Count, 1);
                second.Reset();

                Assert.AreEqual(first.Count, 0);
                Assert.AreEqual(second.Count, 0);
            }

            [Test]
            public void Dependency()
            {
                SingletonUsesAtAnyTime singleton = container[typeof (SingletonUsesAtAnyTime)] as SingletonUsesAtAnyTime;
                Assert.AreEqual(singleton.Count, 0);
                CountIncrementor incrementor = container[typeof (CountIncrementor)] as CountIncrementor;
                Assert.AreEqual(singleton.Count, 1);
            }

            private IWindsorContainer container;
        }
    }

    #endregion

    #endregion
}