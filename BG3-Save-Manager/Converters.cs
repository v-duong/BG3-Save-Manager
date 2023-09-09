using System;
using System.Globalization;
using System.Windows.Data;

namespace BG3_Save_Manager
{
    public class SaveGameTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((SaveMetadata.SaveType)value)
            {
                case SaveMetadata.SaveType.MANUAL:
                    return "Manual";
                case SaveMetadata.SaveType.QUICK:
                    return "Quick";
                case SaveMetadata.SaveType.AUTO:
                    return "Auto";
                default:
                    return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch((string)value)
            {
                case "Manual":
                    return SaveMetadata.SaveType.MANUAL;
                case "Quick":
                    return SaveMetadata.SaveType.QUICK;
                case "Auto":
                    return SaveMetadata.SaveType.AUTO;
                default: return SaveMetadata.SaveType.MANUAL;
            }
        }
    }
}
