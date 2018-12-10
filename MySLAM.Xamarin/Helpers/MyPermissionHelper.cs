using Android.App;
using Android.Content.PM;
using Android.Support.V4.App;
using System.Linq;

namespace MySLAM.Xamarin.Helpers
{
    public class MyPermissionHelper
    {
        private readonly Activity owner;

        public MyPermissionHelper(Activity owner)
        {
            this.owner = owner;
        }

        public bool ConfirmPermissions(string[] permissions, int requestCode)
        {
            var requireList = permissions.ToList()
                                        .Where(a => ActivityCompat.CheckSelfPermission(owner, a) != Permission.Granted);
            if (!requireList.Any())
            {
                return true;
            }
            ActivityCompat.RequestPermissions(
                    owner,
                    requireList.ToArray(),
                    requestCode);
            return false;
        }
    }
}