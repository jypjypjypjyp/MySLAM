using Android.App;
using Android.Content;
using Android.OS;
using System;

namespace MySLAM.Xamarin.Views
{

    public enum DialogType
    {
        Confirmation, Error, Progress, ProgressHorizontal
    }

    public class MyDialog : DialogFragment
    {
        public delegate void ProgressChanged(int progress);
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
            Dialog dialog = null;
            switch (type)
            {
                case DialogType.Confirmation:
                    dialog = new AlertDialog.Builder(Activity)
                            .SetMessage(message)
                            .SetPositiveButton(Resource.String.ok, PositiveHandler)
                            .SetNegativeButton(Resource.String.cancel, NegativeHandler)
                            .Create();
                    break;
                case DialogType.Error:
                    dialog = new AlertDialog.Builder(Activity)
                            .SetMessage(message)
                            .SetPositiveButton(Resource.String.ok, PositiveHandler)
                            .Create();
                    break;
                case DialogType.Progress:
                    dialog = new ProgressDialog(Activity);
                    dialog.SetTitle(Resource.String.progress);
                    dialog.SetCancelable(true);
                    dialog.SetCanceledOnTouchOutside(false);
                    ((ProgressDialog)dialog).SetMessage(message);
                    return dialog;
                case DialogType.ProgressHorizontal:
                    dialog = new ProgressDialog(Activity);
                    dialog.SetTitle(Resource.String.progress);
                    dialog.SetCancelable(true);
                    dialog.SetCanceledOnTouchOutside(false);
                    ((ProgressDialog)dialog).SetMessage(message);
                    ((ProgressDialog)dialog).SetProgressStyle(ProgressDialogStyle.Horizontal);
                    break;
            }
            return dialog;
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            NegativeHandler(null, null);
            base.OnCancel(dialog);
        }
    }
}