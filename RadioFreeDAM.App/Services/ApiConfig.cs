namespace RadioFreeDAM.Services;

/// <summary>
/// Dirección de nuestra API según dónde se ejecute la app.
/// </summary>
public static class ApiConfig
{
    public const int Port = 5080;

    /// <summary>
    /// IP local del ordenador donde corre la API, para probar con un móvil Android real
    /// en la misma red Wi-Fi. Se consulta con <c>ipconfig</c> (Dirección IPv4).
    /// </summary>
    public const string LocalNetworkHost = "192.168.1.100";

    public static string BaseUrl
    {
        get
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // El emulador de Android ve el ordenador anfitrión en 10.0.2.2.
                return DeviceInfo.DeviceType == DeviceType.Virtual
                    ? $"http://10.0.2.2:{Port}/"
                    : $"http://{LocalNetworkHost}:{Port}/";
            }

            // Windows: la API corre en la misma máquina.
            return $"http://localhost:{Port}/";
        }
    }
}
