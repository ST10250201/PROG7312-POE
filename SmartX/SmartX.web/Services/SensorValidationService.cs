using SmartX.Web.Models;

namespace SmartX.Web.Services
{
    public class SensorValidationService
    {
        public List<string> ConvertZonesToList(string[][] zoneMatrix)
        {
            List<string> zones = new List<string>();

            foreach (string[] zoneGroup in zoneMatrix)
            {
                foreach (string zone in zoneGroup)
                {
                    zones.Add(zone);
                }
            }

            return zones;
        }

        public bool ValidateDeploymentTree(List<string> nodes, int index = 0)
        {
            if (index >= nodes.Count)
            {
                return true;
            }

            string currentNode = nodes[index];

            if (string.IsNullOrWhiteSpace(currentNode))
            {
                return false;
            }

            return ValidateDeploymentTree(nodes, index + 1);
        }

        public SensorStatus DetermineStatus(
            double value,
            double warningThreshold,
            double criticalThreshold)
        {
            if (value >= criticalThreshold)
            {
                return SensorStatus.Critical;
            }

            if (value >= warningThreshold)
            {
                return SensorStatus.Warning;
            }

            return SensorStatus.Normal;
        }
    }
}