package communication
{
   import ui.*;
   
   public class BreadCrumb
   {
      private var _screen:Screen;
      
      public function BreadCrumb(arg1:Screen)
      {
         super();
         this._screen = arg1;
      }
      
      public function get screen() : Screen
      {
         return this._screen;
      }
   }
}

