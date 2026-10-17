using SistemaReservas.API.Reglas;
using Xunit;

public class ReglasReservaTests
{
    [Fact]
    public void ValidarHoraFinalizacion_HoraFinMayorQueInicio_RetornaNull()
    {
        // Arrange
        var inicio = new TimeSpan(8, 0, 0);
        var fin = new TimeSpan(10, 0, 0);

        // Act
        var resultado = ReglasReserva.ValidarHoraFinalizacion(inicio, fin);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void ValidarHoraFinalizacion_SinHoraFin_RetornaMensajeRequerida()
    {
        // Arrange
        TimeSpan? inicio = new TimeSpan(8, 0, 0);
        TimeSpan? fin = null;

        // Act
        var resultado = ReglasReserva.ValidarHoraFinalizacion(inicio, fin);

        // Assert
        Assert.Equal(ReglasReserva.MensajeHoraFinRequerida, resultado);
    }

    [Fact]
    public void ValidarHoraFinalizacion_HoraFinIgualAInicio_RetornaMensajeInvalida()
    {
        // Arrange
        var hora = new TimeSpan(9, 0, 0);

        // Act
        var resultado = ReglasReserva.ValidarHoraFinalizacion(hora, hora);

        // Assert
        Assert.Equal(ReglasReserva.MensajeHoraFinInvalida, resultado);
    }

    [Fact]
    public void ValidarHoraFinalizacion_HoraFinMenorQueInicio_RetornaMensajeInvalida()
    {
        // Arrange
        var inicio = new TimeSpan(10, 0, 0);
        var fin = new TimeSpan(8, 0, 0);

        // Act
        var resultado = ReglasReserva.ValidarHoraFinalizacion(inicio, fin);

        // Assert
        Assert.Equal(ReglasReserva.MensajeHoraFinInvalida, resultado);
    }

    [Fact]
    public void ValidarHoraFinalizacion_SinHoraInicio_NoCompara_RetornaNull()
    {
        // Arrange
        TimeSpan? inicio = null;
        TimeSpan? fin = new TimeSpan(10, 0, 0);

        // Act
        var resultado = ReglasReserva.ValidarHoraFinalizacion(inicio, fin);

        // Assert
        Assert.Null(resultado);
    }
}