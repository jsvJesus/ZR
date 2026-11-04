package communication
{
   import ui.Screen;
   
   public class BreadCrumb
   {
      private var _screen:Screen;
      
      public function BreadCrumb(screen:Screen)
      {
         super();
         this._screen = screen;
      }
      
      public function get screen() : Screen
      {
         return this._screen;
      }
   }
}

