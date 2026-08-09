namespace GestionGastos.Herramientas;

/// <summary>
/// La cuenta dedicada que se usa para medir los no funcionales (AC-32 a AC-34). Es una
/// sola y fija, para que sembrar y medir hablen de lo mismo sin pasarse el mail de la mano.
///
/// El dominio es `@ejemplo.test` a proposito: es el criterio de
/// `backend/db/003-borrar-usuarios-de-prueba.sql`, asi que la limpieza al terminar ya
/// esta resuelta y no hay que acordarse de borrarla a mano.
/// </summary>
public static class CuentaDeCarga
{
    public const string Email = "carga@ejemplo.test";

    /// <summary>
    /// No es un secreto: la cuenta existe solo en la base de desarrollo y se borra al
    /// terminar. Va escrita aca justamente para que nadie la ponga en user-secrets y se
    /// confunda con una credencial de verdad.
    /// </summary>
    public const string Contrasena = "CargaDePrueba123!";
}
