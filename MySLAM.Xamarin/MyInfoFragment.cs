
using Android.App;
using Android.OS;
using Android.Views;

namespace MySLAM.Xamarin
{
    public class MyInfoFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.info_fragment, container, false);
        }
    }
}