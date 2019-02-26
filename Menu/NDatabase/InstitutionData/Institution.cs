using System;
using Menu.NDatabase;
using Newtonsoft.Json.Linq;

namespace Menu.NDatabase.InstitutionData
{
    public class Institution
    {
        public int institution_id;
        public int owner_id;
        public string institution_name;
        public string institution_about;
        public double institution_location_x;
        public double institution_location_y;
        public string institution_image;
        public string institution_email;
        public string institution_phone_number;
        public JArray institution_schedule;
    }
}
