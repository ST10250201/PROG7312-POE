namespace SmartX.Api.Services
{
    public class RecursiveValidationService
    {
        public bool ValidateHierarchy(
            List<string> nodes,
            int index = 0)
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

            return ValidateHierarchy(
                nodes,
                index + 1);
        }
    }
}