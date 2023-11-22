using System.Net.Http.Headers;
using System.Text;
using APIClient.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult ChooseRole(string role)
    {
        if (role == "foretag")
        {
            return RedirectToAction("CreateCompanyAdvertiser");
        }
        else if (role == "prenumerant")
        {
            return RedirectToAction("GetSubscriber");
        }
        else
        {
            return RedirectToAction("Index");
        }
    }
    [HttpGet]
    public IActionResult CreateCompanyAdvertiser()
    {
        AnnonsorModel model = new AnnonsorModel();
        return View(model);
    }
    
    [HttpGet]
    public IActionResult CreateSubscriberAdvertiser()
    {
        return View("CreateSubscriberAdvertiser");
    }

    public IActionResult GetSubscriber()
    {
        return View("GetSubscriber");
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriber(int prenumerantId)
    {
        var subscriberInfo = await GetPrenumerantById(prenumerantId);

        if (subscriberInfo != null)
        {
            return View("GetSubscriber", subscriberInfo);
        }
        else
        {
            ViewBag.ErrorMessage = "Felaktigt prenumerationsnummer eller problem med att hämta uppgifter.";
            return View("GetSubscriber");
        }
    }

    public async Task<PrenumerantModel> GetPrenumerantById(int prenumerantId)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = $"http://localhost:5284/Prenumerant/id?id={prenumerantId}";
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                PrenumerantModel prenumerant = JsonConvert.DeserializeObject<PrenumerantModel>(apiResponse);
                return prenumerant;
            }
            else
            {
                return null;
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateSubscriberAdvertiser(int prenumerantId)
    {
        var prenumerantInfo = await GetPrenumerantById(prenumerantId);

        if (prenumerantInfo != null)
        {
            AnnonsorModel annonsor = new AnnonsorModel
            {
                PrenumerantId = prenumerantInfo.Prenumerationsnummer,
                ForetagsAnnonsor = false,
                Namn = $"{prenumerantInfo.Fornamn} {prenumerantInfo.Efternamn}",
                Telefonnummer = prenumerantInfo.Telefonnummer,
                Utdelningsadress = prenumerantInfo.Utdelningsadress,
                Postnummer = prenumerantInfo.Postnummer,
                Ort = prenumerantInfo.Ort,
                Fakturaadress = null,
                Organisationsnummer = null
            };

            int result = await CreateAnnonsor(annonsor);

            if (result > 0)
            {
                HttpContext.Session.SetInt32("PrenumerantId", (int)annonsor.PrenumerantId);
                var identifier = HttpContext.Session.GetInt32("PrenumerantId") ?? 0;
                annonsor = await GetAnnonsorByPrenumerantId(identifier);
                ViewBag.SuccessMessage = "Annonsör skapad!";
                ViewBag.AnnonsButtonVisible = "true";
            }
            else
            {
                ViewBag.ErrorMessage = "Problem med att skapa annonsör.";
                ViewBag.AnnonsButtonVisible = "false";
            }

            return View("CreateSubscriberAdvertiser", prenumerantInfo);
        }
        else
        {
            return RedirectToAction("GetSubscriber", new { prenumerantId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompanyAdvertiser(AnnonsorModel annonsor)
    {
        annonsor.ForetagsAnnonsor = true;
        annonsor.PrenumerantId = 0;

        int result = await CreateAnnonsor(annonsor);

        if (result > 0)
        {
            HttpContext.Session.SetString("Organisationsnummer", annonsor.Organisationsnummer);
            var identifier = HttpContext.Session.GetString("Organisationsnummer");
            annonsor = await GetAnnonsorByOrganisationsnummer(identifier);
            ViewData["SuccessMessage"] = "Annonsör skapad utifrån företag!";
            ViewData["AnnonsButtonVisible"] = "true";
        }
        else
        {
            ViewData["ErrorMessage"] = "Problem med att skapa annonsör utifrån företag.";
            ViewData["AnnonsButtonVisible"] = "false";
        }

        return View("CreateCompanyAdvertiser", annonsor);
    }

    private async Task<int> CreateAnnonsor(AnnonsorModel annonsor)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = "http://localhost:5030/Annonsor";

            string jsonBody = JsonConvert.SerializeObject(annonsor);
            StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                return 1;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Content: {errorContent}");
                return 0;
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateAd()
    {
        try
        {
            var identifier = HttpContext.Session.GetString("Organisationsnummer");

            if (string.IsNullOrEmpty(identifier))
            {
                ViewData["ErrorMessage"] = "Organisationsnummer not found.";
                //ViewData["AnnonsId"] = 0;
                return View("CreateAd");
            }

            AnnonsorModel annonsor = await GetAnnonsorByOrganisationsnummer(identifier);

            if (annonsor != null)
            {
                AnnonsModel annonsModel = new AnnonsModel
                {
                    AnnonsorId = annonsor.AnnonsorId,
                };
                return View(annonsModel);
            }
            else
            {
                ViewData["ErrorMessage"] = "Annonsor not found.";
                ViewData["AnnonsId"] = 0;
                return View("CreateAd");
            }
        }
        catch (Exception ex)
        {
            ViewData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            ViewData["AnnonsId"] = 0;
            return View("CreateAd");
        }
    }


    [HttpPost]
    public async Task<IActionResult> CreateAd(AnnonsModel annons)
    {
        try
        {
            var identifier = HttpContext.Session.GetString("Organisationsnummer") ??
                             HttpContext.Session.GetInt32("PrenumerantId")?.ToString();

            if (string.IsNullOrEmpty(identifier))
            {
                ViewData["ErrorMessage"] = "Identifier not found.";
                ViewData["AnnonsId"] = 0;
                return View("CreateAd");
            }

            AnnonsorModel annonsor = int.TryParse(identifier, out _) ?
                await GetAnnonsorByPrenumerantId(int.Parse(identifier)) :
                await GetAnnonsorByOrganisationsnummer(identifier);

            if (annonsor != null)
            {
                annons.AnnonsorId = annonsor.AnnonsorId;
            }
            else
            {
                ViewData["ErrorMessage"] = "Annonsor not found.";
                ViewData["AnnonsId"] = 0;
                return View("CreateAd");
            }

            /*int result = await InsertAnnons(new AnnonsModel
            {
                AnnonsorId = annons.AnnonsorId,
                Rubrik = annons.Rubrik,
                Innehall = annons.Innehall,
                VaransPris = annons.VaransPris
            });*/
            int result = await InsertAnnons(annons);

            if (result > 0)
            {
                ViewData["SuccessMessage"] = "Annons skapad!";
                //ViewData["AnnonsId"] = result;
            }
            else
            {
                ViewData["ErrorMessage"] = "Problem med att skapa annons.";
                //ViewData["AnnonsId"] = 0;
            }

            return View("CreateAd");
        }
        catch (Exception ex)
        {
            ViewData["ErrorMessage"] = $"Ett fel uppstod: {ex.Message}";
            //ViewData["AnnonsId"] = 0;
            return View("CreateAd");
        }
    }

    private async Task<int> InsertAnnons(AnnonsModel annons)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = "http://localhost:5030/Annons";

            string jsonBody = JsonConvert.SerializeObject(annons);
            StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                return 1;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Content: {errorContent}");
                return 0;
            }
        }
    }

    public async Task<IActionResult> ListOfAds()
    {
        List<AnnonsModel> annonsList = await GetAllAnnonser();
        return View(annonsList);
    }

    private async Task<List<AnnonsModel>> GetAllAnnonser()
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = "http://localhost:5030/Annons/AllAnnonser";

            client.BaseAddress = new Uri(apiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                List<AnnonsModel> annonsList = JsonConvert.DeserializeObject<List<AnnonsModel>>(apiResponse);
                return annonsList;
            }
            else
            {
                return new List<AnnonsModel>();
            }
        }
    }

    public async Task<AnnonsorModel> GetAnnonsorByPrenumerantId(int prenumerantId)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = $"http://localhost:5284/Annonsor/prenumerantId?prenumerantId={prenumerantId}";
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                AnnonsorModel annonsor = JsonConvert.DeserializeObject<AnnonsorModel>(apiResponse);
                return annonsor;
            }
            else
            {
                return null;
            }
        }
    }

    public async Task<AnnonsorModel> GetAnnonsorByOrganisationsnummer(string organisationsnummer)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl =
                $"http://localhost:5284/Annonsor/organisationsnummer?organisationsnummer={organisationsnummer}";
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                AnnonsorModel annonsor = JsonConvert.DeserializeObject<AnnonsorModel>(apiResponse);
                return annonsor;
            }
            else
            {
                return null;
            }
        }
    }
}