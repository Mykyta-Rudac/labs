using NetSdrClientApp.Infrastructure;

namespace NetSdrClientApp.UI
{
    // This class intentionally depends on Infrastructure to trigger the architectural rule
    public class BadUiComponent
    {
        private readonly InfraService _service;

        public BadUiComponent()
        {
            _service = new InfraService();
        }

        public string Render() => _service.GetInfo();
    }
}
