using System.Text;

using SearchAdsLocation.Data;

using Xunit;

namespace SearchAdsLocation.Tests
{
    public class AdsServiceTests
    {
        [Fact]
        public async Task UploadAsync_ValidData_ShouldAddLocations()
        {
            // Arrange
            var adsService = new AdsService();
            var data = "Google:/RU/Moscow, /RU/Moscow/Lenina\nYandex:/RU/Moscow";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            // Act
            await adsService.UploadAsync(stream);

            // Assert
            var result1 = await adsService.SearchAsync("/RU/Moscow");
            var result2 = await adsService.SearchAsync("/RU/Moscow/Lenina");

            Assert.Contains("Google", result1);
            Assert.Contains("Yandex", result1);
            Assert.Contains("Google", result2);
            Assert.DoesNotContain("Yandex", result2);
        }

        [Fact]
        public async Task UploadAsync_EmptyOrInvalidLines_ShouldIgnoreThem()
        {
            // Arrange
            var adsService = new AdsService();
            var data = "\n\n: /RU/Empty\nGoogle:\nYandex:/RU/Moscow";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            // Act
            await adsService.UploadAsync(stream);

            // Assert
            var result = await adsService.SearchAsync("/RU/Moscow");
            Assert.Contains("Yandex", result);
            Assert.DoesNotContain("Google", result);
        }

        [Theory]
        [InlineData("/RU/Moscow", new[] { "Google", "Yandex" })]
        [InlineData("/RU/Moscow/Lenina", new[] { "Google" })]
        [InlineData("/RU/Unknown", new string[0])]
        public async Task SearchAsync_ShouldReturnCorrectPlatforms(string query, string[] expected)
        {
            // Arrange
            var adsService = new AdsService();
            var data = "Google:/RU/Moscow, /RU/Moscow/Lenina\nYandex:/RU/Moscow";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            await adsService.UploadAsync(stream);

            // Act
            var result = await adsService.SearchAsync(query);

            // Assert
            Assert.Equal(expected.OrderBy(x => x), result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchAsync_NullOrEmptyQuery_ReturnsEmpty(string query)
        {
            // Arrange
            var adsService = new AdsService();

            // Act
            var result = await adsService.SearchAsync(query);

            // Assert
            Assert.Empty(result);
        }
    }
}