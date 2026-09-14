namespace SmartX.Api.Services
{
    public class DeploymentZoneService
    {
        public List<string> ConvertJaggedArrayToList(string[][] zones)
        {
            List<string> result = new();

            foreach (string[] zoneGroup in zones)
            {
                foreach (string zone in zoneGroup)
                {
                    result.Add(zone);
                }
            }

            return result;
        }
    }
}