package EmotionsGUI_fla
{
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   
   public dynamic class ramka_left_9 extends MovieClip
   {
      public function ramka_left_9()
      {
         super();
      }
      
      public function onOver(e:MouseEvent) : *
      {
         gotoAndStop("over");
      }
      
      public function onOut(e:MouseEvent) : *
      {
         gotoAndStop("out");
      }
   }
}

