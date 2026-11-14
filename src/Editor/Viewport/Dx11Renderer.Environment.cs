using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Environment;
using Editor.Viewport.Environment;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int MaximumWeatherParticles = 2048;

        private EnvironmentSettings _environmentSettings = new();
        private SkyRenderer? _skyRenderer;

        private Buffer? _environmentLightBuffer;
        private Buffer? _weatherVertexBuffer;

        private BlendState? _weatherBlend;
        private DepthStencilState? _weatherDepth;

        private WeatherType _loadedWeather = (WeatherType)(-1);

        private readonly WeatherRuntime _weatherRuntime = new();
        private readonly WeatherOcclusion _weatherOcclusion = new();

        private readonly Dictionary<
            WeatherType,
            (float Rate, float FallSpeed)> _weatherEmitterCache = new();

        private WeatherFrame _weatherFrame;
        private SkyCycle? _skyCycle;

        private float _particleRate;
        private float _particleFallSpeed;
        private float _environmentTime;
        private long _lastEnvironmentFrame;

        public void SetEnvironment(EnvironmentSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (!ReferenceEquals(_environmentSettings, settings))
            {
                _weatherRuntime.Reset();
            }

            _environmentSettings = settings;
            _loadedWeather = (WeatherType)(-1);
        }

        private void InitializeEnvironment()
        {
            _environmentLightBuffer = new Buffer(
                _device,
                Utilities.SizeOf<EnvironmentLightConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            _weatherVertexBuffer = new Buffer(
                _device,
                Utilities.SizeOf<GridVertex>() *
                MaximumWeatherParticles * 2,
                ResourceUsage.Dynamic,
                BindFlags.VertexBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                0);

            var blend = BlendStateDescription.Default();

            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend =
                BlendOption.SourceAlpha;
            blend.RenderTarget[0].DestinationBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].BlendOperation =
                BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend =
                BlendOption.One;
            blend.RenderTarget[0].DestinationAlphaBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].AlphaBlendOperation =
                BlendOperation.Add;

            _weatherBlend = new BlendState(_device, blend);

            var depth = DepthStencilStateDescription.Default();
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.DepthComparison = Comparison.LessEqual;

            _weatherDepth = new DepthStencilState(
                _device,
                depth);

            _skyRenderer = new SkyRenderer(
                _device,
                _context,
                AssetPaths.FromApplication());

            _skyCycle = SkyCycle.Load(AssetPaths.FromApplication());
            InitializeShadowResources();
        }

        private void AdvanceEnvironmentTime()
        {
            long current = Stopwatch.GetTimestamp();
            float deltaTime = 0f;

            if (_lastEnvironmentFrame != 0)
            {
                deltaTime = (float)(
                    (double)(current - _lastEnvironmentFrame) /
                    Stopwatch.Frequency);

                deltaTime = Math.Clamp(deltaTime, 0f, 0.1f);
            }

            _lastEnvironmentFrame = current;
            _environmentTime += deltaTime;

            if (_environmentSettings.RunTimeOfDay)
            {
                float secondsPerHour = Math.Clamp(
                    _environmentSettings.SecondsPerGameHour,
                    1f,
                    3600f);

                float time = _environmentSettings.TimeOfDay +
                             deltaTime / secondsPerHour;

                _environmentSettings.TimeOfDay =
                    (time % 24f + 24f) % 24f;
            }

            _weatherFrame = _weatherRuntime.Advance(
                _environmentSettings,
                deltaTime);

            float rainIntensity = Math.Clamp(
                _weatherFrame.Rain * _environmentSettings.Precipitation,
                0f,
                1f);

            _terrain?.UpdateWeatherWetness(
                rainIntensity,
                deltaTime);

            _roads?.AdvanceWeatherWetness(
                rainIntensity,
                deltaTime,
                _environmentSettings.UniformRoadWetness);
        }

        private Vector3 UpdateEnvironmentLighting(
            Vector3 cameraPosition,
            out float daylight)
        {
            var settings = _environmentSettings;

            float time = Math.Clamp(
                settings.TimeOfDay,
                0f,
                24f);

            float hourAngle = (time - 12f)
                * MathF.PI / 12f;

            float elevation = MathF.Cos(hourAngle);
            float horizontal = MathF.Sin(hourAngle);

            float azimuth = settings.SunAzimuth
                * MathF.PI / 180f;

            Vector3 direction = Vector3.Normalize(new Vector3(
                MathF.Cos(azimuth) * horizontal,
                elevation,
                MathF.Sin(azimuth) * horizontal));

            daylight = Math.Clamp(
                (direction.Y + 0.05f) * 4f,
                0f,
                1f);

            Vector3 sunColor = _skyCycle?.SunColor(time) ??
                Vector3.Lerp(
                    new Vector3(1f, 0.53f, 0.26f),
                    new Vector3(1f, 0.96f, 0.87f),
                    daylight);

            Vector3 ambient = _skyCycle?.AmbientColor(time) ??
                Vector3.Lerp(
                    new Vector3(0.07f, 0.09f, 0.14f),
                    new Vector3(0.48f, 0.52f, 0.56f),
                    daylight);

            ambient *= Math.Clamp(
                settings.AmbientIntensity,
                0f,
                2f);

            float sunlight = daylight
                * Math.Clamp(settings.SunIntensity, 0f, 3f)
                * _weatherFrame.SunMultiplier;

            float lightning = _weatherFrame.Lightning;

            if (lightning > 0f)
            {
                ambient += new Vector3(
                    0.75f,
                    0.85f,
                    1f) * lightning * 0.85f;
            }

            float fogDensity = Math.Clamp(
                _weatherFrame.FogDensity,
                0f,
                0.04f);

            Vector3 fogColor = Vector3.Lerp(
                new Vector3(0.018f, 0.028f, 0.045f),
                new Vector3(0.56f, 0.64f, 0.69f),
                daylight);

            fogColor *= 1f -
                _weatherFrame.StormCover * 0.45f;

            fogColor += new Vector3(
                0.35f,
                0.43f,
                0.6f) * lightning * 0.35f;

            var constants = new EnvironmentLightConstants
            {
                Sun = new Vector4(direction, sunlight),
                SunColor = new Vector4(sunColor, 1f),
                Ambient = new Vector4(ambient, 1f),
                Fog = new Vector4(fogColor, fogDensity),
                Camera = new Vector4(cameraPosition, 1f)
            };

            _context.UpdateSubresource(
                ref constants,
                _environmentLightBuffer!);

            _context.PixelShader.SetConstantBuffer(
                1,
                _environmentLightBuffer);

            return direction;
        }

        private void RenderEnvironmentSky(
            FreeCamera camera,
            Vector3 sunDirection,
            float daylight,
            bool playing)
        {
            _skyRenderer?.Render(
                camera,
                _environmentSettings,
                _weatherFrame,
                sunDirection,
                daylight,
                _width,
                _height,
                playing ? _playFieldOfView : 1.05f,
                _environmentTime);
        }

        private void RenderEnvironmentWeather(
            FreeCamera camera,
            Matrix4x4 viewProjection)
        {
            if (_weatherVertexBuffer == null ||
                _weatherBlend == null ||
                _weatherDepth == null)
            {
                return;
            }

            if (_weatherFrame.Rain > 0.001f ||
                _weatherFrame.Snow > 0.001f)
            {
                _weatherOcclusion.Prepare(
                    _scene,
                    camera.Position,
                    35f,
                    _environmentSettings.WindSpeed);
            }

            WeatherType rainType =
                _environmentSettings.Weather == WeatherType.Thunderstorm
                    ? WeatherType.Thunderstorm
                    : WeatherType.Rain;

            WeatherType snowType =
                _environmentSettings.Weather == WeatherType.Blizzard
                    ? WeatherType.Blizzard
                    : WeatherType.Snow;

            RenderWeatherLayer(
                camera,
                viewProjection,
                rainType,
                _weatherFrame.Rain);

            RenderWeatherLayer(
                camera,
                viewProjection,
                snowType,
                _weatherFrame.Snow);
        }

        private void RenderWeatherLayer(
            FreeCamera camera,
            Matrix4x4 viewProjection,
            WeatherType weather,
            float amount)
        {
            if (amount <= 0.001f)
            {
                return;
            }

            bool snow = weather is WeatherType.Snow or
                WeatherType.Blizzard;

            UpdateWeatherEmitter(weather);

            float intensity = Math.Clamp(
                _environmentSettings.Precipitation * amount,
                0f,
                2f);

            int particleCount = Math.Clamp(
                (int)(_particleRate * 5f * intensity),
                0,
                MaximumWeatherParticles);

            if (particleCount == 0)
            {
                return;
            }

            var vertices = new GridVertex[particleCount * 2];

            float windAngle = _environmentSettings.WindDirection
                * MathF.PI / 180f;

            Vector3 wind = new Vector3(
                MathF.Cos(windAngle),
                0f,
                MathF.Sin(windAngle));

            wind *= _environmentSettings.WindSpeed;

            float fallSpeed = MathF.Max(
                _particleFallSpeed,
                0.5f);

            float extent = snow ? 35f : 28f;

            Vector3 forward = Vector3.Normalize(
                camera.LookDirection);

            Vector3 right = Vector3.Cross(
                Vector3.UnitY,
                forward);

            if (right.LengthSquared() < 0.001f)
            {
                right = Vector3.UnitX;
            }
            else
            {
                right = Vector3.Normalize(right);
            }

            RawColor4 color = snow
                ? new RawColor4(0.94f, 0.96f, 1f, 0.85f)
                : new RawColor4(0.7f, 0.8f, 0.92f, 0.48f);

            int vertexCount = 0;

            for (int i = 0; i < particleCount; i++)
            {
                float x = (Hash(i * 17 + 3) * 2f - 1f)
                    * extent;

                float z = (Hash(i * 29 + 7) * 2f - 1f)
                    * extent;

                float phase = Hash(i * 43 + 11)
                    * extent;

                float y = PositiveModulo(
                    phase - _environmentTime * fallSpeed,
                    extent) - extent * 0.5f;

                float fallTime = (extent * 0.5f - y)
                    / fallSpeed;

                Vector3 position = camera.Position +
                    new Vector3(x, y, z) +
                    wind * fallTime * (snow ? 0.4f : 0.18f);

                if (Vector3.Dot(
                        position - camera.Position,
                        forward) <= 0.25f)
                {
                    continue;
                }

                Vector3 offset = snow
                    ? right * 0.09f +
                      new Vector3(0f, -0.09f, 0f)
                    : new Vector3(
                        wind.X * 0.025f,
                        -0.65f,
                        wind.Z * 0.025f);

                Vector3 end = position + offset;

                if (!_weatherOcclusion.Clip(
                        ref position,
                        ref end))
                {
                    continue;
                }

                vertices[vertexCount++] = new GridVertex(
                    position,
                    color);

                vertices[vertexCount++] = new GridVertex(
                    end,
                    color);
            }

            if (vertexCount == 0)
            {
                return;
            }

            _context.MapSubresource(
                _weatherVertexBuffer,
                MapMode.WriteDiscard,
                SharpDX.Direct3D11.MapFlags.None,
                out DataStream stream);

            try
            {
                using (stream)
                {
                    stream.WriteRange(
                        vertices,
                        0,
                        vertexCount);
                }
            }
            finally
            {
                _context.UnmapSubresource(
                    _weatherVertexBuffer,
                    0);
            }

            _context.UpdateSubresource(
                ref viewProjection,
                _cameraBuffer);

            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.LineList;

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    _weatherVertexBuffer,
                    Utilities.SizeOf<GridVertex>(),
                    0));

            _context.VertexShader.Set(_vertexShader);
            _context.VertexShader.SetConstantBuffer(
                0,
                _cameraBuffer);

            _context.PixelShader.Set(_pixelShader);

            _context.OutputMerger.SetDepthStencilState(
                _weatherDepth);

            _context.OutputMerger.SetBlendState(
                _weatherBlend);

            _context.Rasterizer.State = null;
            _context.Draw(vertexCount, 0);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
        }

        private void UpdateWeatherEmitter(WeatherType weather)
        {
            if (_loadedWeather == weather)
            {
                return;
            }

            _loadedWeather = weather;

            if (_weatherEmitterCache.TryGetValue(
                weather,
                out var cached))
            {
                _particleRate = cached.Rate;
                _particleFallSpeed = cached.FallSpeed;
                return;
            }

            bool snow = weather is WeatherType.Snow or
                WeatherType.Blizzard;

            _particleRate = snow ? 32f : 130f;
            _particleFallSpeed = snow ? 3.2f : 18f;

            string filename = weather switch
            {
                WeatherType.Rain => "camera_rain.xml",
                WeatherType.Thunderstorm => "camera_rain.xml",
                WeatherType.Snow => "snow_flakes.xml",
                WeatherType.Blizzard => "blizzard.xml",
                _ => ""
            };

            if (filename.Length > 0)
            {
                string path = Path.Combine(
                    AssetPaths.FromApplication().Root,
                    "particles",
                    filename);

                if (File.Exists(path))
                {
                    try
                    {
                        var root = PackedSection.Read(path);

                        PackedSection? emitter = root.Children
                            .SelectMany(section =>
                                section.Find("Actions")?.Children ??
                                Array.Empty<PackedSection>())
                            .FirstOrDefault(
                                action => action.Name == "Source");

                        var rate = emitter?
                            .Find("rate_")?.Value as float[];

                        if (rate is { Length: > 0 } &&
                            float.IsFinite(rate[0]) &&
                            rate[0] > 0f)
                        {
                            _particleRate = Math.Clamp(
                                rate[0],
                                1f,
                                600f);
                        }

                        var velocityGenerator = emitter?
                            .Find("pVelocitySrc")?
                            .Children.FirstOrDefault();

                        var first = velocityGenerator?
                            .Find("corner_")?.Value as float[];

                        var second = velocityGenerator?
                            .Find("opposite_")?.Value as float[];

                        if (first is { Length: >= 3 } &&
                            second is { Length: >= 3 })
                        {
                            float speed = -0.5f *
                                (first[1] + second[1]);

                            if (speed > 0.5f && speed < 100f)
                            {
                                _particleFallSpeed = speed;
                            }
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // Используем параметры по умолчанию.
                    }
                }
            }

            _weatherEmitterCache[weather] = (
                _particleRate,
                _particleFallSpeed);
        }

        private static float PositiveModulo(
            float value,
            float modulus)
        {
            return (value % modulus + modulus) % modulus;
        }

        private static float Hash(int seed)
        {
            uint value = unchecked((uint)seed);

            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            value ^= value >> 16;

            return (value & 0x00ffffff) / 16777216f;
        }

        private void DisposeEnvironment()
        {
            _weatherOcclusion.Dispose();
            DisposeShadowResources();

            _skyRenderer?.Dispose();
            _weatherDepth?.Dispose();
            _weatherBlend?.Dispose();
            _weatherVertexBuffer?.Dispose();
            _environmentLightBuffer?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EnvironmentLightConstants
        {
            public Vector4 Sun;
            public Vector4 SunColor;
            public Vector4 Ambient;
            public Vector4 Fog;
            public Vector4 Camera;
        }
    }
}
