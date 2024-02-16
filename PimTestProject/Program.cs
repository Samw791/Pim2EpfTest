using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PimTestProject;

class Program
{
    private static readonly string UrlForAllSeries = "https://pim.trox.de/admin/rest/view/children/16439/?limit=200";
    private static readonly string UrlForProductID = "https://pim.trox.de/admin/rest/view/reference/";
    private static readonly string UrlForReference = "https://pim.trox.de/admin/rest/product/";
    private const int AttributeID = 962;
    private static string Username;
    private static string Password;

    static async Task Main(string[] args)
    {
        if (!ReadCredentials())
        {                                                     
            Console.WriteLine("Failed to read Credentials");
            return;
        }

        await GetAllSeries();

        int productID;
        bool validInput = false;

        while (!validInput)
        {
            Console.WriteLine("Welche ProduktID wollen Sie haben: ");

            if (!int.TryParse(Console.ReadLine(), out productID))
            {
                Console.WriteLine("Ungültige Eingabe für Produkt-ID. Bitte geben Sie eine gülitge ein.");
            }
            else
            {
                JObject json = await SearchForProductID(productID);
                if (json != null)
                {
                    await GetFormattedValue(json);
                    validInput = true;
                }
                else
                {
                    Console.WriteLine("Produkt-ID nicht gefunden. Bitte geben Sie eine andere Produkt-ID ein.");
                }
            }

        }
        Console.ReadLine();
    }
    static bool ReadCredentials()
    {
        return CredentialsReader.ReadCredentials(out Username, out Password);
    }
    private static HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = new HttpClient();
        string authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        return client;
    }
    private static async Task<JObject> HandleResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
        else
        {
            Console.WriteLine("Response Failed. Status code: " + response.StatusCode);
            return null;
        }
    }
    private static async Task GetAllSeries()
    {
        using (HttpClient client = CreateAuthenticatedClient())
        {
            HttpResponseMessage response = await client.GetAsync(UrlForAllSeries);
            JObject jObject = await HandleResponse(response);
            Console.WriteLine(jObject.ToString());
        }
    }

    private static async Task<JObject> SearchForProductID(int productID)
    {
        Console.Clear();
        using (HttpClient client = CreateAuthenticatedClient())
        {
            HttpResponseMessage response = await client.GetAsync($"{UrlForProductID}{productID}/{AttributeID}");
            return await HandleResponse(response);
        }
    }

    private static async Task GetFormattedValue(JObject ProductReferenc)
    {
        Console.WriteLine("Searching for the Product-ID.");
        JObject references = ProductReferenc["References"] as JObject;
        string productID=string.Empty;
        if (references != null)
        {
            foreach (KeyValuePair<string, JToken> reference in references)
            {
                JObject referenceValue = reference.Value as JObject;

                if (referenceValue != null && referenceValue["TargetItem"] != null && referenceValue["TargetItem"]["ID"] != null)
                {
                    productID = referenceValue["TargetItem"]["ID"].ToString();
                    Console.WriteLine("Product-ID found.");
                    break;
                }
            }
        }

        using (HttpClient client = CreateAuthenticatedClient())
        {
            HttpResponseMessage response = await client.GetAsync(UrlForReference + productID);
            JObject jObject = await HandleResponse(response);
            if (jObject == null)
            {
                return;
            }
            Console.WriteLine(jObject.ToString());
        }
    }

}
