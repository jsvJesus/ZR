package ui
{
   import flash.display.Bitmap;
   
   public class PremiumIcons
   {
      public function PremiumIcons()
      {
         super();
      }
      
      public static function byId(id:int) : Bitmap
      {
         switch(id)
         {
            case 0:
               return new Bitmap(new x2(),"auto",true);
            case 1:
               return new Bitmap(new x3(),"auto",true);
            default:
               return new Bitmap(new defaultIcon(),"auto",true);
         }
      }
   }
}

