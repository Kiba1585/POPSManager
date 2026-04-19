using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POPSManager.ViewModels
{
    /// <summary>
    /// Clase base para ViewModels que implementa INotifyPropertyChanged.
    /// Proporciona un método helper para establecer propiedades con notificación automática.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica que una propiedad ha cambiado.
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad (se obtiene automáticamente).</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Establece el valor de un campo y notifica el cambio si el valor es diferente.
        /// </summary>
        /// <typeparam name="T">Tipo de la propiedad.</typeparam>
        /// <param name="field">Referencia al campo privado.</param>
        /// <param name="value">Nuevo valor.</param>
        /// <param name="propertyName">Nombre de la propiedad (opcional, se infiere automáticamente).</param>
        /// <returns>true si el valor cambió; false en caso contrario.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}