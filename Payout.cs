using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class Payout
{
    static async Task Main()
    {
        // Demo endpoint
        string url = "https://ppp-test.nuvei.com/ppp/api/v1/payout.do";
        Random random = new Random();
        // Get from PaymentIQ's (DevCode's) Transfer API response
        // in TradeNetworks.Live.CreditCardDepositCommunicationLogs
        // Add serviceMapping to PIQ's Transfer API response
        int userTokenId = 11848420;
        string timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string merchantSecretKey = "puT8KQYqIbbQDHN5cQNAlYyuDedZxRYjA9WmEsKq1wrIPhxQqOx77Ep1uOA7sUde";

        string merchantId = "3832456837996201334";
        string merchantSiteId = "184063";
        string clientRequestId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string amount = "100";
        string currency = "USD";
        // Use SHA256 to calculate the checksum. See the class below.
        string checksum = CalculateChecksum(merchantId, merchantSiteId, clientRequestId, amount, currency, timeStamp, merchantSecretKey);

        var jsonBody = new
        {
            merchantId,
            merchantSiteId,
            clientRequestId,
            userTokenId,
            clientUniqueId = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + userTokenId,
            amount,
            currency,
            timeStamp,
            // Get from PaymentIQ's (DevCode's) Transfer API response
            // in TradeNetworks.Live.CreditCardDepositCommunicationLogs
            // Add serviceMapping to PIQ's Transfer API response
            userPaymentOption = new { userPaymentOptionId = "363202111" },
            deviceDetails = new { ipAddress = GetLocalIPAddress() },
            checksum
        };

        // The JSON body is ready. Now, send the request to the endpoint.
        using var client = new HttpClient();
        try
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(url, jsonBody);
            string responseString = await response.Content.ReadAsStringAsync();

            // Print the request and response to the console
            Console.WriteLine("// Request:");
            Console.WriteLine("// POST " + url);
            Console.WriteLine(JsonSerializer.Serialize(jsonBody, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine();

            Console.WriteLine("// Response:");
            try
            {
                // Format the JSON response
                string formattedResponse = JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<object>(responseString),
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine(formattedResponse);
            }
            catch (JsonException)
            {
                // If the response is not a valid JSON, print it as-is
                Console.WriteLine(responseString);
            }

            Console.WriteLine();
            Console.WriteLine($"Response Status Code: {response.StatusCode}");

            // If the response is an error, print the error code and reason
            using JsonDocument doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("errCode", out JsonElement errCodeElement) &&
                doc.RootElement.TryGetProperty("reason", out JsonElement reasonElement))
            {
                Console.WriteLine($"Error Code: {errCodeElement.GetInt32()}");
                Console.WriteLine($"Reason: {reasonElement.GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    // Get the local IP address of the machine
    static string GetLocalIPAddress()
    {
        using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            if (endPoint == null)
            {
                throw new InvalidOperationException("Failed to get local IP address.");
            }
            return endPoint.Address.ToString();
        }
    }

    // Calculate the checksum using SHA256
    static string CalculateChecksum(string merchantId, string merchantSiteId, string clientRequestId, string amount, string currency, string timeStamp, string merchantSecretKey)
    {
        string rawData = merchantId + merchantSiteId + clientRequestId + amount + currency + timeStamp + merchantSecretKey;
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        StringBuilder builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}