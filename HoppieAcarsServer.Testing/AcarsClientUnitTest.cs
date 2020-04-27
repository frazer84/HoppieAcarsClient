using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HoppieAcarsClient.Testing
{
    public class AcarsClientUnitTest
    {
        private const string LOGON_SECRET = "abc123";
        private const string CALLSIGN = "SAS123";

        private const string HOPPIE_CALLSIGNS_ONLINE = "ok { SAS123 SAS321 ESSA_V_TWR ESSA_ATIS }";
        private const string ACARS_MESSAGES_MULTIPLE_MIXED = "ok {SWSM cpdlc {/data2/14//R/AT @ELTOK@ EXPECT @300K}} {SWSM cpdlc {/data2/15//NE/NEXT DATA AUTHORITY @EDYY}} {SWSM cpdlc {/data2/16//NE/CONFIRM ALTITUDE}} {SWSM telex {TELEX TEST}} {SWSM telex {TELEX TEST}} {SWSM telex {TELEX TEST}} {SWSM cpdlc {/data2/17//WU/CROSS @SPL@ AT @5000}} {SWSM cpdlc {/data2/18//R/ATIS @T}}";
        private const string ACARS_MESSAGES_SINGLE_TELEX = "ok {SWSM telex {TELEX TEST}}";
        private const string ACARS_MESSAGES_SINGLE_CPDLC = "ok {SWSM cpdlc {/data2/14//R/AT @ELTOK@ EXPECT @300K}}";

        public HttpClient GetMockedHttpClient(HttpStatusCode statusCode, string responseData)
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                    // Setup the PROTECTED method to mock
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    // prepare the expected response of the mocked http call
                    .ReturnsAsync(new HttpResponseMessage()
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(responseData),
                    })
                    .Verifiable();

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://www.hoppie.nl/"),
            };
            return httpClient;
        }

        [Fact]
        public async void TestGetAllCallsignsOnline()
        {
            var acarsClient = new AcarsClient(
                CALLSIGN,
                LOGON_SECRET, 
                false, 
                GetMockedHttpClient(HttpStatusCode.OK, HOPPIE_CALLSIGNS_ONLINE)
            );

            string[] callsigns = await acarsClient.GetAllCallsignsOnline().ConfigureAwait(false);
            Assert.NotNull(callsigns);
            Assert.True(callsigns.Length == 4);
            Assert.Equal("SAS123", callsigns[0]);
        }

        [Fact]
        public async void TestRecieveAcarsMessage()
        {
            var acarsClient = new AcarsClient(
                CALLSIGN, 
                LOGON_SECRET, 
                false, 
                GetMockedHttpClient(HttpStatusCode.OK, ACARS_MESSAGES_MULTIPLE_MIXED)
            );

            // ACT
            AcarsMessage[] result = await acarsClient.GetPendingMessages().ConfigureAwait(false);

            // ASSERT
            Assert.NotNull(result);
            Assert.True(result.Length == 8);

            Assert.Equal("SWSM", result[0].From);
            Assert.Equal(AcarsClient.MessageType.CPDLC, result[0].Type);
            Assert.Equal(CALLSIGN, result[0].To);
            Assert.Equal("/data2/14//R/AT @ELTOK@ EXPECT @300K", result[0].Data);

            Assert.Equal("SWSM", result[3].From);
            Assert.Equal(AcarsClient.MessageType.Telex, result[3].Type);
            Assert.Equal(CALLSIGN, result[3].To);
            Assert.Equal("TEST", result[3].Data);

            /*
            // also check the 'http' call was like we expected it
            var expectedUri = new Uri("http://test.com/api/test/whatever");

            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1), // we expected a single external request
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get  // we expected a GET request
                  && req.RequestUri == expectedUri // to this uri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
            */
        }
    }
}
