#if !MONO
namespace Nancy.Hosting.Self.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeItEasy;

    using Nancy.Bootstrapper;
    using Nancy.Tests;
    using Nancy.Tests.xUnitExtensions;

    using Xunit;
    using Xunit.Abstractions;

    /// <remarks>
    /// These tests attempt to listen on port 1234, and so require either administrative
    /// privileges or that a command similar to the following has been run with
    /// administrative privileges:
    /// <code>netsh http add urlacl url=http://+:1234/base user=DOMAIN\user</code>
    /// See http://msdn.microsoft.com/en-us/library/ms733768.aspx for more information.
    /// </remarks>
    public class NancySelfHostFixture
    {
        private readonly ITestOutputHelper testOutputHelper;

        public NancySelfHostFixture(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        private static readonly Uri BaseUri = new Uri("http://localhost:1234/base/");

        [SkippableFact]
        public void Should_be_get_an_exception_indicating_a_conflict_when_trying_to_listen_on_a_used_prefix()
        {
            Exception ex;

            // Given
            using (CreateAndOpenSelfHost())
            {
                // When
                ex = Record.Exception(() =>
                    {
                        using (var host = new NancyHost(BaseUri))
                        {
                            host.Start();
                        }
                    });
            }

            // Then
            ex.Message.ShouldContain("conflict");
        }

        [SkippableFact]
        public async Task Should_be_able_to_get_any_header_from_selfhost()
        {
            // Given
            using (CreateAndOpenSelfHost())
            {
                // When
                var client = new HttpClient();
                var response = await client.GetAsync(new Uri(BaseUri, "rel/header/?query=value"));
                var matches = response.Headers.Where(val => val.Key == "X-Some-Header").SelectMany(val => val.Value);
                // Then
                matches.Single().ShouldEqual("Some value");
            }
        }

        [SkippableFact]
        public async Task Should_set_query_string_and_uri_correctly()
        {
            // Given
            Request nancyRequest = null;
            var fakeEngine = A.Fake<INancyEngine>();
            A.CallTo(() => fakeEngine.HandleRequest(A<Request>.Ignored, A<Func<NancyContext, NancyContext>>.Ignored,A<CancellationToken>.Ignored))
                .Invokes(f => nancyRequest = (Request)f.Arguments[0])
                .ReturnsLazily(c => Task.FromResult(new NancyContext { Request = (Request)c.Arguments[0], Response = new Response() }));

            var fakeBootstrapper = A.Fake<INancyBootstrapper>();
            A.CallTo(() => fakeBootstrapper.GetEngine()).Returns(fakeEngine);

            // When
            using (CreateAndOpenSelfHost(fakeBootstrapper))
            {
                var client = new HttpClient();
                await client.GetAsync(new Uri(BaseUri, "test/stuff?query=value&query2=value2"));
            }

            // Then
            nancyRequest.Path.ShouldEqual("/test/stuff");
            Assert.True(nancyRequest.Query.query.HasValue);
            Assert.True(nancyRequest.Query.query2.HasValue);
        }

        [SkippableFact]
        public async Task Should_be_able_to_get_from_selfhost()
        {
            using (CreateAndOpenSelfHost())
            {
                var client = new HttpClient();
                var reader = new StreamReader(await client.GetStreamAsync(new Uri(BaseUri, "rel")));
                var response = reader.ReadToEnd();

                response.ShouldEqual("This is the site route");
            }
        }

        [SkippableFact]
        public async Task Should_be_able_to_get_from_chunked_selfhost()
        {
            using (CreateAndOpenSelfHost())
            {
                var client = new HttpClient();
                var response = await client.GetAsync(new Uri(BaseUri, "rel"));

                // Then
                var chunked = response.Headers.Where(val => val.Key == "Transfer-Encoding").SelectMany(val => val.Value);
                var contentLength = response.Headers.Where(val => val.Key == "Content-Length").SelectMany(val => val.Value);
                chunked.Single().ShouldEqual("chunked");
                contentLength.Count().ShouldEqual(0);

                using (var reader = new StreamReader(await client.GetStreamAsync(new Uri(BaseUri, "rel"))))
                {
                    var contents = reader.ReadToEnd();
                    contents.ShouldEqual("This is the site route");
                }
            }
        }

        [SkippableFact]
        public void Should_be_able_to_get_from_contentlength_selfhost()
        {
            var configuration = new HostConfiguration()
            {
                AllowChunkedEncoding = false
            };
            using (CreateAndOpenSelfHost(null, configuration))
            {
                var response = WebRequest.Create(new Uri(BaseUri, "rel")).GetResponse();

                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(22, Convert.ToInt32(response.Headers["Content-Length"]));

                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var contents = reader.ReadToEnd();
                    contents.ShouldEqual("This is the site route");
                }
            }
        }

        [SkippableFact]
        public async Task Should_be_able_to_post_body_to_selfhost()
        {
            using (CreateAndOpenSelfHost())
            {
                const string testBody = "This is the body of the request";

                var client = new HttpClient();
                using (var response = await client.PostAsync(new Uri(BaseUri, "rel"), new StringContent(testBody)))
                {
                    var responseBody = new StreamReader(await response.Content.ReadAsStreamAsync());
                    responseBody.ShouldEqual(testBody);
                }
            }
        }

        [SkippableFact]
        public async Task Should_be_able_to_get_from_selfhost_with_slashless_uri()
        {
            using (CreateAndOpenSelfHost())
            {
                var client = new HttpClient();

                var reader = new StreamReader(await client.GetStreamAsync(BaseUri.ToString().TrimEnd('/')));
                var response = reader.ReadToEnd();
                response.ShouldEqual("This is the site home");
            }
        }

        private static NancyHostWrapper CreateAndOpenSelfHost(INancyBootstrapper nancyBootstrapper = null, HostConfiguration configuration = null)
        {
            if (nancyBootstrapper == null)
            {
                nancyBootstrapper = new DefaultNancyBootstrapper();
            }

            var host = new NancyHost(
                nancyBootstrapper,
                configuration,
                BaseUri);

            try
            {
                host.Start();
            }
            catch
            {
                throw new SkipException("Skipped due to no Administrator access - please see test fixture for more information.");
            }

            return new NancyHostWrapper(host);
        }


        [SkippableFact]
        public async Task Should_be_able_to_recover_from_rendering_exception()
        {
            using (CreateAndOpenSelfHost())
            {
                var client = new HttpClient();

                var reader = new StreamReader(await client.GetStreamAsync(new Uri(BaseUri, "exception")));
                var response = reader.ReadToEnd();
                response.ShouldEqual("Content");
            }
        }

        [SkippableFact]
        public void Should_be_serializable()
        {
            var type = typeof(NancyHost);
            var test = type.Attributes.ToString().Contains("Serializable");
            Assert.True(test);
        }

        [Fact]
        public void Should_include_default_port_in_uri_prefixes()
        {
            // Given
            var host = new NancyHost(new Uri("http://localhost/"));

            // When
            var prefix = host.GetPrefixes().Single();

            // Then
            prefix.ShouldEqual("http://+:80/");
        }

        [Fact]
        public void Should_not_throw_when_disposed_without_starting()
        {
            // Given
            var bootstrapperMock = A.Fake<INancyBootstrapper>();
            var host = new NancyHost(new Uri("http://localhost/"), bootstrapperMock);

            // When
            host.Dispose();

            // Then
            A.CallTo(() => bootstrapperMock.Dispose()).MustHaveHappened();
        }

        private class NancyHostWrapper : IDisposable
        {
            private readonly NancyHost host;

            public NancyHostWrapper(NancyHost host)
            {
                this.host = host;
            }

            public void Dispose()
            {
                host.Stop();
            }
        }
    }
}
#endif
