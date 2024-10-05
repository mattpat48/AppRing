using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Platforms.Android.Android_Auto.Screens
{
    public class AAScreenMenu : Screen
    {
        public AAScreenMenu(CarContext carContext) : base(carContext)
        {
        }

        public override ITemplate OnGetTemplate()
        {
            var listTemplate = new ItemList.Builder()
                .SetNoItemsMessage("No items")
                .Build();

            return new ListTemplate.Builder()
                .SetTitle("Ring")
                .SetSingleList(listTemplate)
                .Build();
        }
    }
}
