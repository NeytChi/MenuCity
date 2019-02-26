using System;
using Newtonsoft.Json.Linq;

namespace Menu.NDatabase.DishData
{
    public class Dish
    {
        public int dish_id;
        public string dish_name;
        public string dish_about;
        public string dish_image;
        public int created_at;
        public int updated_at;
        public JArray array_ingredients;
    }
}
