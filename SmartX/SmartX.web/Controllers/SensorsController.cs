using Microsoft.AspNetCore.Mvc;
using SmartX.Web.Models;
using SmartX.Web.Services;

namespace SmartX.Web.Controllers
{
    public class SensorsController : Controller
    {
        private static readonly List<Sensor> _sensors = new();
        private readonly SensorValidationService _validationService;

        public SensorsController()
        {
            _validationService = new SensorValidationService();
        }

        // Displays all registered sensors
        public IActionResult Index()
        {
            return View(_sensors);
        }

        // Displays the sensor registration form
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Receives and validates a new sensor registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Sensor sensor)
        {
            if (_sensors.Any(s =>
                s.MacAddress.Equals(
                    sensor.MacAddress,
                    StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(
                    nameof(sensor.MacAddress),
                    "A sensor with this MAC address is already registered.");
            }

            string[][] deploymentZones =
            {
                new[] { "Greenhouse", "Hydroponics" },
                new[] { "Utility", "Storage" },
                new[] { "Production", "Testing" }
            };

            List<string> zones =
                _validationService.ConvertZonesToList(deploymentZones);

            bool deploymentStructureValid =
                _validationService.ValidateDeploymentTree(zones);

            if (!deploymentStructureValid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The sensor deployment structure is invalid.");
            }

            if (!ModelState.IsValid)
            {
                return View(sensor);
            }

            sensor.Id = _sensors.Count == 0
                ? 1
                : _sensors.Max(s => s.Id) + 1;

            _sensors.Add(sensor);

            TempData["SuccessMessage"] =
                "Sensor registered successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}