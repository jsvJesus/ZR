package communication
{
   import ui.*;
   
   public class BreadCrumb
   {
      private var _screen:Screen;
      
      public function BreadCrumb(param1:Screen)
      {
         super();
         this._screen = param1;
      }
      
      public function get screen() : Screen
      {
         return this._screen;
      }
   }
}

