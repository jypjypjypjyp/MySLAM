using Android.App;
using Android.Content;
using Android.OS;
using System;

namespace MySLAM.Xamarin.MyView
{

    public enum DialogType
    {
        Confirmation, Error
    }

    public class MyDialog : DialogFragment
    {
        public EventHandler<DialogClickEventArgs> PositiveHandler { get; set; }
        public EventHandler<DialogClickEventArgs> NegativeHandler { get; set; }

        private DialogType type;
        private string message;

        public MyDialog(DialogType type, string message)
        {
            this.type = type;
            this.message = message;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            switch (type)
            {
                case DialogType.Confirmation:
                    return new AlertDialog.Builder(Activity)
                            .SetMessage(message)
                            .SetPositiveButton(Resource.String.ok, PositiveHandler)
                            .SetNegativeButton(Resource.String.cancel, NegativeHandler)
                            .Create();
                case DialogType.Error:
                    return new AlertDialog.Builder(Activity)
                            .SetMessage(message)
                            .SetPositiveButton(Resource.String.ok, PositiveHandler)
                            .Create();
                default:
                    return base.OnCreateDialog(savedInstanceState);
            }
        }
    }
}