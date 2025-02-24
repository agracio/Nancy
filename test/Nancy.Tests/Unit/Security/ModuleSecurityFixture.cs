
namespace Nancy.Tests.Unit.Security
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeItEasy;

    using Nancy.Responses;
    using Nancy.Security;
    using Nancy.Tests.Fakes;

    using Xunit;

    public class ModuleSecurityFixture
    {
        private async void TestForbidden(string method)
        {
            var module = new FakeHookedModule(new BeforePipeline());
            var url = GetFakeUrl(false);
            var context = new NancyContext
            {
                Request = new Request(method, url)
            };

            module.RequiresHttps();

            // When
            var result = await module.Before.Invoke(context, new CancellationToken());

            // Then
            result.ShouldNotBeNull();
            result.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);

        }

        private async void TestForbidden(FakeHookedModule module, NancyContext context)
        {
            // When
            var result = await module.Before.Invoke(context, new CancellationToken());

            // Then
            result.ShouldNotBeNull();
            result.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);

        }

        private async void TestNull(FakeHookedModule module, NancyContext context)
        {
            // When
            var result = await module.Before.Invoke(context, new CancellationToken());

            // Then
            result.ShouldBeNull();
        }

        [Fact]
        public void Should_add_an_item_to_the_end_of_the_begin_pipeline_when_RequiresAuthentication_enabled()
        {
            var module = new FakeHookedModule(A.Fake<BeforePipeline>());

            module.RequiresAuthentication();

            A.CallTo(() => module.Before.AddItemToEndOfPipeline(A<Func<NancyContext, Response>>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void Should_add_two_items_to_the_end_of_the_begin_pipeline_when_RequiresClaims_enabled()
        {
            var module = new FakeHookedModule(A.Fake<BeforePipeline>());

            module.RequiresClaims(_ => true);

            A.CallTo(() => module.Before.AddItemToEndOfPipeline(A<Func<NancyContext, Response>>.Ignored)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task Should_return_unauthorized_response_with_RequiresAuthentication_enabled_and_no_user()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAuthentication();

            var result = await module.Before.Invoke(new NancyContext(), new CancellationToken());

            result.ShouldNotBeNull();
            result.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_return_unauthorized_response_with_RequiresAuthentication_enabled_and_no_identity()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAuthentication();

            var context = new NancyContext
            {
                CurrentUser = new ClaimsPrincipal()
            };

            var result = await module.Before.Invoke(context, new CancellationToken());

            result.ShouldNotBeNull();
            result.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_return_null_with_RequiresAuthentication_enabled_and_user_provided()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAuthentication();

            var context = new NancyContext
            {
                CurrentUser = GetFakeUser("Bob")
            };
            TestNull(module, context);
        }

        [Fact]
        public async Task Should_return_forbidden_response_with_RequiresClaims_enabled_but_nonmatching_claims()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresClaims(c => c.Type == "Claim1");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser(
                    "username",
                    new Claim("Claim2", string.Empty),
                    new Claim("Claim3", string.Empty))
            };

            TestForbidden(module, context);
        }

        [Fact]
        public async Task Should_return_forbidden_response_with_RequiresClaims_enabled_but_claims_key_missing()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresClaims(c => c.Type == "Claim1");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser("username")
            };

            TestForbidden(module, context);
        }

        [Fact]
        public async Task Should_return_forbidden_response_with_RequiresClaims_enabled_but_not_all_claims_met()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresClaims(c => c.Type == "Claim1", c => c.Type == "Claim2");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser(
                    "username",
                    new Claim("Claim2", string.Empty))
            };

            TestForbidden(module, context);
        }

        [Fact]
        public async Task Should_return_null_with_RequiresClaims_and_all_claims_met()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresClaims(c => c.Type == "Claim1", c => c.Type == "Claim2");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser("username",
                new Claim("Claim1", string.Empty),
                new Claim("Claim2", string.Empty),
                new Claim("Claim3", string.Empty))
            };

            TestNull(module, context);
        }


        [Fact]
        public async Task Should_return_forbidden_response_with_RequiresAnyClaim_enabled_but_nonmatching_claims()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAnyClaim(c => c.Type == "Claim1");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser(
                    "username",
                    new Claim("Claim2", string.Empty),
                    new Claim("Claim3", string.Empty))
            };

            TestForbidden(module, context);
        }

        [Fact]
        public async Task Should_return_forbidden_response_with_RequiresAnyClaim_enabled_but_claims_key_missing()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAnyClaim(c => c.Type == "Claim1");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser("username")
            };

            TestForbidden(module, context);
        }

        [Fact]
        public async Task Should_return_null_with_RequiresAnyClaim_and_any_claim_met()
        {
            var module = new FakeHookedModule(new BeforePipeline());
            module.RequiresAnyClaim(c => c.Type == "Claim1", c => c.Type == "Claim4");
            var context = new NancyContext
            {
                CurrentUser = GetFakeUser("username",
                    new Claim("Claim1", string.Empty),
                    new Claim("Claim2", string.Empty),
                    new Claim("Claim3", string.Empty))
            };

            TestNull(module, context);
        }

        [Fact]
        public async Task Should_return_redirect_response_when_request_url_is_non_secure_method_is_get_and_requires_https()
        {
            // Given
            var module = new FakeHookedModule(new BeforePipeline());
            var url = GetFakeUrl(false);
            var context = new NancyContext
            {
                Request = new Request("GET", url)
            };

            module.RequiresHttps();

            // When
            var result = await module.Before.Invoke(context, new CancellationToken());

            // Then
            result.ShouldNotBeNull();
            result.ShouldBeOfType<RedirectResponse>();

            url.Scheme = "https";
            url.Port = null;
            result.Headers["Location"].ShouldEqual(url.ToString());
        }

        [Fact]
        public async Task Should_return_redirect_response_with_specific_port_number_when_request_url_is_non_secure_method_is_get_and_requires_https()
        {
            // Given
            var module = new FakeHookedModule(new BeforePipeline());
            var url = GetFakeUrl(false);
            var context = new NancyContext
            {
                Request = new Request("GET", url)
            };

            module.RequiresHttps(true, 999);

            // When
            var result = await module.Before.Invoke(context, new CancellationToken());

            // Then
            result.ShouldNotBeNull();
            result.ShouldBeOfType<RedirectResponse>();

            url.Scheme = "https";
            url.Port = 999;
            result.Headers["Location"].ShouldEqual(url.ToString());
        }

        [Fact]
        public async Task Should_return_forbidden_response_when_request_url_is_non_secure_method_is_post_and_requires_https()
        {
            TestForbidden("POST");
        }

        [Fact]
        public async Task Should_return_forbidden_response_when_request_url_is_non_secure_method_is_delete_and_requires_https()
        {
            TestForbidden("DELETE");
        }

        [Fact]
        public async Task Should_return_forbidden_response_when_request_url_is_non_secure_method_is_get_and_requires_https_and_redirect_is_false()
        {
            TestForbidden("GET");
        }

        [Fact]
        public async Task Should_return_forbidden_response_when_request_url_is_non_secure_method_is_post_and_requires_https_and_redirect_is_false()
        {
            TestForbidden("POST");
        }

        [Fact]
        public async Task Should_return_null_response_when_request_url_is_secure_method_is_get_and_requires_https()
        {
            TestForbidden("GET");
        }

        [Fact]
        public async Task Should_return_null_response_when_request_url_is_secure_method_is_post_and_requires_https()
        {
            // Given
            var module = new FakeHookedModule(new BeforePipeline());
            var url = GetFakeUrl(true);
            var context = new NancyContext
            {
                Request = new Request("POST", url)
            };

            module.RequiresHttps();

            TestNull(module, context);
        }

        private static ClaimsPrincipal GetFakeUser(string userName, params Claim[] claims)
        {
            var claimsList = claims.ToList();
            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, userName));

            return new ClaimsPrincipal(new ClaimsIdentity(claimsList, "test"));
        }

        private static Url GetFakeUrl(bool https)
        {
            return new Url
            {
                BasePath = null,
                HostName = "localhost",
                Path = "/",
                Port = 80,
                Query = string.Empty,
                Scheme = https ? "https" : "http"
            };
        }
    }
}
