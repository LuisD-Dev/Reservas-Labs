using MapsterMapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Domain.Rules;

namespace SistemaReservas.Application.Implementation
{
    public class LaboratorioService : ILaboratorioService
    {
        private readonly IMapper _mapper;
        private readonly ILaboratorioRepository _laboratorioRepository;

        // NFR2 - Separar acceso a datos: el acceso a Laboratorios está en ILaboratorioRepository.
        public LaboratorioService(ILaboratorioRepository laboratorioRepository, IMapper mapper)
        {
            _mapper = mapper;
            _laboratorioRepository = laboratorioRepository;
        }

        public async Task<IReadOnlyList<LaboratorioResponse>> ObtenerLaboratoriosAsync()
        {
            return _mapper.Map<List<LaboratorioResponse>>(await _laboratorioRepository.ObtenerTodosAsync());
        }

        public async Task<ResultadoActualizarEstadoLaboratorio> ActualizarEstadoAsync(int id, string? estado)
        {
            if (id <= 0)
                return new(EstadoActualizacionLaboratorio.DatosInvalidos, "El identificador del laboratorio debe ser mayor que cero.");

            if (!ReglasEstadoLaboratorio.EsEstadoValido(estado))
                return new(EstadoActualizacionLaboratorio.DatosInvalidos, "El estado debe ser Habilitado o Fuera de servicio.");

            var laboratorio = await _laboratorioRepository.ObtenerPorIdAsync(id);
            if (laboratorio is null)
                return new(EstadoActualizacionLaboratorio.NoEncontrado, "El laboratorio no existe.");

            laboratorio.Estado = estado!;
            await _laboratorioRepository.GuardarCambiosAsync();

            return new(EstadoActualizacionLaboratorio.Exitosa,
                "Estado del laboratorio actualizado correctamente.",
                _mapper.Map<LaboratorioResponse>(laboratorio));
        }
    }
}
