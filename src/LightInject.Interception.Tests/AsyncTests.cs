﻿namespace LightInject.Interception.Tests
{
    using System;
    using System.CodeDom;
    using System.Configuration;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;
    [Collection("Interception")]
    public class AsyncTests
    {
        [Fact]
        public async Task ShouldInvokeAsyncTask()
        {
            var targetMock = new Mock<IMethodWithTaskReturnValue>();                        
            var proxy = CreateProxy(targetMock.Object);

            await proxy.Execute();

            targetMock.Verify(m => m.Execute(),Times.Once);
        }

        [Fact]
        public async Task ShouldInvokeAsyncOfTTask()
        {
            Mock<IMethodWithTaskOfTReturnValue> targetMock = new Mock<IMethodWithTaskOfTReturnValue>();
            targetMock.Setup(m => m.Execute()).ReturnsAsync(42);
            var proxy = CreateProxy(targetMock.Object);

            var result = await proxy.Execute();

            Assert.Equal(42, result);
        }

        [Fact]
        public void ShouldInvokeSynchronousMethod()
        {
            Mock<IMethodWithNoParameters> targetMock = new Mock<IMethodWithNoParameters>();
            var proxy = CreateProxy(targetMock.Object);

            proxy.Execute();

            targetMock.Verify(m => m.Execute(), Times.Once);
        }

        [Fact]
        public async Task ShouldInterceptAsyncTaskMethod()
        {
            var sampleInterceptor = new SampleAsyncInterceptor();            
            var targetMock = new Mock<IMethodWithTaskReturnValue>();
            var proxy = CreateProxy(targetMock.Object, sampleInterceptor);
            await proxy.Execute();    
            Assert.True(sampleInterceptor.InterceptedTaskMethod);        
        }

        [Fact]
        public async Task ShouldInterceptAsyncTaskOfTMethod()
        {
            var sampleInterceptor = new SampleAsyncInterceptor();
            var targetMock = new Mock<IMethodWithTaskOfTReturnValue>();
            var proxy = CreateProxy(targetMock.Object, sampleInterceptor);
            await proxy.Execute();
            Assert.True(sampleInterceptor.InterceptedTaskOfTMethod);
        }


        private T CreateProxy<T>(T target)
        {
            ProxyBuilder proxyBuilder = new ProxyBuilder();
            ProxyDefinition proxyDefinition = new ProxyDefinition(typeof(T), () => target);
            proxyDefinition.Implement(() => new SampleAsyncInterceptor());
            var proxyType = proxyBuilder.GetProxyType(proxyDefinition);
            var proxy = (T)Activator.CreateInstance(proxyType);
            return proxy;
        }

        private T CreateProxy<T>(T target, IInterceptor interceptor)
        {
            ProxyBuilder proxyBuilder = new ProxyBuilder();
            ProxyDefinition proxyDefinition = new ProxyDefinition(typeof(T), () => target);
            proxyDefinition.Implement(() => interceptor);
            var proxyType = proxyBuilder.GetProxyType(proxyDefinition);
            var proxy = (T)Activator.CreateInstance(proxyType);
            return proxy;
        }
    }



    internal class SampleAsyncInterceptor : AsyncInterceptor
    {
        public bool InterceptedTaskOfTMethod;

        public bool InterceptedTaskMethod;
        
        public override object Invoke(IInvocationInfo invocationInfo)
        {
            // Before method invocation            
            var value = base.Invoke(invocationInfo);            
            // After method invocation
            return value;
        }

        protected override async Task InvokeAsync(IInvocationInfo invocationInfo)
        {
            InterceptedTaskMethod = true;
            // Before method invocation
            await base.InvokeAsync(invocationInfo);
            // After method invocation
        }

        protected override async Task<T> InvokeAsync<T>(IInvocationInfo invocationInfo)
        {
            InterceptedTaskOfTMethod = true;
            // Before method invocation
            var value = await base.InvokeAsync<T>(invocationInfo);
            // After method invocation           
            return value;
        }
    }

    public interface IMethodWithTaskReturnValue
    {
        Task Execute();
    }

    public interface IMethodWithTaskOfTReturnValue
    {
        Task<int> Execute();
    }    
}