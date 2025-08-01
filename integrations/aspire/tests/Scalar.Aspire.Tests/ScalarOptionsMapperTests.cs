namespace Scalar.Aspire.Tests;

public class ScalarOptionsMapperTests
{
    [Fact]
    public void ToScalarConfigurations_ShouldReturnCorrectDefaultConfiguration()
    {
        // Arrange
        ScalarTestOptions[] options = [new()];

        // Act
        var configurations = options.ToScalarConfigurations();

        // Assert
        var configuration = configurations.First();
        configuration.ProxyUrl.Should().BeNull();
        configuration.ShowSidebar.Should().BeTrue();
        configuration.SidebarShowPath.Should().BeFalse();
        configuration.HideModels.Should().BeFalse();
        configuration.HideTestRequestButton.Should().BeFalse();
        configuration.DarkMode.Should().BeTrue();
        configuration.ForceDarkModeState.Should().BeNull();
        configuration.HideDarkModeToggle.Should().BeFalse();
        configuration.CustomCss.Should().BeNull();
        configuration.SearchHotKey.Should().BeNull();
        configuration.Servers.Should().BeNull();
        configuration.MetaData.Should().BeNull();
        configuration.DefaultHttpClient!.TargetKey.Should().Be("shell");
        configuration.DefaultHttpClient!.ClientKey.Should().Be("curl");
        configuration.HiddenClients.Should().BeNull();
        configuration.Authentication.Should().BeNull();
        configuration.WithDefaultFonts.Should().BeTrue();
        configuration.DefaultOpenAllTags.Should().BeFalse();
        configuration.TagsSorter.Should().BeNull();
        configuration.OperationsSorter.Should().BeNull();
        configuration.Theme.Should().BeNull();
        configuration.Integration.Should().Be("dotnet");
        configuration.Sources.Should().BeEmpty();
        configuration.PersistAuth.Should().BeFalse();
        configuration.DocumentDownloadType.Should().BeNull();
    }

    [Fact]
    public void ToConfiguration_ShouldReturnCorrectCustomConfiguration()
    {
        // Arrange
        var options = new ScalarTestOptions
        {
            ProxyUrl = "http://localhost:8080",
            ShowSidebar = false,
            HideModels = true,
            HideTestRequestButton = true,
            DarkMode = false,
            ForceThemeMode = ThemeMode.Light,
            HideDarkModeToggle = true,
            CustomCss = "*{}",
            SearchHotKey = "o",
            Theme = ScalarTheme.Saturn,
            Layout = ScalarLayout.Classic,
            Servers = [new ScalarServer("https://example.com")],
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
            DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient),
            HiddenClients = true,
            DefaultFonts = false,
            DefaultOpenAllTags = true,
            TagSorter = TagSorter.Alpha,
            OperationSorter = OperationSorter.Method,
            HideClientButton = true,
            PersistentAuthentication = true,
            DocumentDownloadType = DocumentDownloadType.Json
        };
        options.AddDocument("v2");

        // Act
        var configuration = options.ToScalarConfiguration();

        // Assert
        configuration.ProxyUrl.Should().Be("http://localhost:8080");
        configuration.ShowSidebar.Should().BeFalse();
        configuration.HideModels.Should().BeTrue();
        configuration.HideTestRequestButton.Should().BeTrue();
        configuration.DarkMode.Should().BeFalse();
        configuration.HideDarkModeToggle.Should().BeTrue();
        configuration.CustomCss.Should().Be("*{}");
        configuration.SearchHotKey.Should().Be("o");
        configuration.Servers.Should().ContainSingle().Which.Url.Should().Be("https://example.com");
        configuration.MetaData.Should().ContainKey("key").WhoseValue.Should().Be("value");
        configuration.DefaultHttpClient!.TargetKey.Should().Be("csharp");
        configuration.DefaultHttpClient!.ClientKey.Should().Be("httpclient");
        ((bool) configuration.HiddenClients!).Should().BeTrue();
        configuration.WithDefaultFonts.Should().BeFalse();
        configuration.DefaultOpenAllTags.Should().BeTrue();
        configuration.TagsSorter.Should().Be("alpha");
        configuration.OperationsSorter.Should().Be("method");
        configuration.Theme.Should().Be("saturn");
        configuration.Layout.Should().Be("classic");
        configuration.Integration.Should().Be("dotnet");
        configuration.HideClientButton.Should().BeTrue();
        configuration.Sources.Should().ContainSingle().Which.Url.Should().Be("openapi/v2.json");
        configuration.PersistAuth.Should().BeTrue();
        configuration.DocumentDownloadType.Should().Be("json");
    }

    [Fact]
    public void GetHiddenClients_ShouldReturnNull_WhenEnabledTargetsAndEnabledClientsAreEmpty()
    {
        // Arrange
        var options = new ScalarTestOptions();

        // Act
        var hiddenClients = options.ToScalarConfiguration().HiddenClients;

        // Assert
        hiddenClients.Should().BeNull();
    }

    [Fact]
    public void GetHiddenClients_ShouldReturnTrue_WhenHiddenClientsIsTrue()
    {
        // Arrange
        var options = new ScalarTestOptions { HiddenClients = true };

        // Act
        var hiddenClients = (bool) options.ToScalarConfiguration().HiddenClients!;

        // Assert
        hiddenClients.Should().BeTrue();
    }

    [Fact]
    public void GetHiddenClients_ShouldReturnFilteredClients_WhenEnabledTargetsIsNotEmpty()
    {
        // Arrange
        var options = new ScalarTestOptions { EnabledTargets = [ScalarTarget.CSharp] };

        // Act
        var hiddenClients = (IDictionary<string, IEnumerable<string>>) options.ToScalarConfiguration().HiddenClients!;

        // Assert
        hiddenClients.Should().HaveCount(ScalarOptionsMapper.ClientOptions.Count - 1);
        hiddenClients.Should().NotContainKey("csharp");
    }

    [Fact]
    public void GetHiddenClients_ShouldReturnFilteredClients_WhenEnabledClientsIsNotEmpty()
    {
        // Arrange
        var options = new ScalarTestOptions { EnabledClients = [ScalarClient.HttpClient, ScalarClient.Python3] };

        // Act
        var hiddenClients = (IDictionary<string, IEnumerable<string>>) options.ToScalarConfiguration().HiddenClients!;

        // Assert
        hiddenClients.Should().HaveCount(ScalarOptionsMapper.ClientOptions.Count);
        hiddenClients.Should().ContainKey("csharp")
            .WhoseValue.Should().ContainSingle().Which.Should().Be("restsharp");
        hiddenClients.Should().ContainKey("python")
            .WhoseValue.Should().ContainSingle().Which.Should().Be("requests");
    }

    [Fact]
    public void GetHiddenClients_ShouldNotReturnTarget_WhenAllClientsAreEnabled()
    {
        // Arrange
        var options = new ScalarTestOptions { EnabledClients = [ScalarClient.OkHttp] }; // All Kotlin clients are enabled

        // Act
        var hiddenClients = (IDictionary<string, IEnumerable<string>>) options.ToScalarConfiguration().HiddenClients!;

        // Assert
        hiddenClients.Should().HaveCount(ScalarOptionsMapper.ClientOptions.Count - 1);
        hiddenClients.Should().NotContainKey("kotlin");
    }

    [Fact]
    public void GetSources_ShouldUseCorrectPattern_WhenCustomPatternIsProvided()
    {
        // Arrange
        var options = new ScalarTestOptions();
        options
            .AddDocument("default")
            .AddDocument("custom", routePattern: "/external/{documentName}.json");

        // Act 
        var configuration = options.ToScalarConfiguration();

        // Assert
        configuration.Sources.Should()
            .SatisfyRespectively(
                first => first.Url.Should().Be("openapi/default.json"),
                second => second.Url.Should().Be("external/custom.json")
            );
    }
}