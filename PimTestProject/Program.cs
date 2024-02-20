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
        bool running = true;

        while (running)
        {
            Console.WriteLine("Welche ProduktID wollen Sie haben: ");

            if (!int.TryParse(Console.ReadLine(), out productID))
            {
                Console.WriteLine("Ungültige Eingabe für Produkt-ID. Bitte geben Sie eine gültige ein.");
            }
            else
            {
                JObject json = await SearchForProductID(productID);
                Console.WriteLine(json.ToString());
                if (json != null)
                {
                    JObject jsonObj = await GetReferenceVKText(json);
                    Console.WriteLine(jsonObj.ToString());
                    GetFormattedValue(jsonObj);
                }
                else
                {
                    Console.WriteLine("Produkt-ID nicht gefunden. Bitte geben Sie eine andere Produkt-ID ein.");
                }
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("Again? (Y/N)");
            if (!(Console.ReadLine().ToUpper()=="Y"))
            {
                running = false;
            }
            Console.Clear();

        }
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

    private static async Task<JObject> GetReferenceVKText(JObject ProductReferenc)
    {
        JObject references = ProductReferenc["References"] as JObject;
        string productID = string.Empty;
        if (references != null)
        {
            foreach (KeyValuePair<string, JToken> reference in references)
            {
                JObject referenceValue = reference.Value as JObject;

                if (referenceValue != null && referenceValue["TargetItem"] != null && referenceValue["TargetItem"]["ID"] != null)
                {
                    productID = referenceValue["TargetItem"]["ID"].ToString();
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
                return null;
            }
            else
            {
                return jObject;
            }
        }
    }
    private static void GetFormattedValue(JObject formattedValueReference)
    {
        Console.WriteLine("Geben Sie die ID für 'Bestellschlüssel JSON für EPF' ein:");
        int idToFilter;
        bool validSearch = false;
        while (!validSearch)
        {
            if (!int.TryParse(Console.ReadLine(), out idToFilter))
            {
                Console.WriteLine("Ungültige Eingabe für ID. Bitte geben Sie eine gültige ein.");
                continue;
            }

            JToken attributes = formattedValueReference.SelectToken("Product.Attributes");

            if (attributes != null)
            {

                foreach (JToken attribute in attributes)
                {

                    if (attribute["ID"].ToString() == idToFilter.ToString())
                    {
                        string formattedValue = attribute["FormattedValue"].ToString();
                        Console.WriteLine($"Formatted Value für ID {idToFilter}: {formattedValue}");
                        return;
                    }
                }

                Console.WriteLine($"Das Attribut mit der ID {idToFilter} wurde nicht gefunden. Bitte geben Sie eine ID ein zu der es auch ein Attribut gibt.");
            }

            else
            {
                Console.WriteLine("Das 'Attributes'-Objekt wurde nicht gefunden.");
                return;
            }
        }       
    }


}