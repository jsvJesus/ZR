package ui.components
{
   import com.dvalimona.components.*;
   import flash.events.*;
   
   public class noDiagrid extends Diagrid
   {
      public function noDiagrid()
      {
         super();
      }
      
      private function draw() : void
      {
         this.graphics.clear();
         this.graphics.beginFill(0,0.8);
         this.graphics.drawRect(0,0,width,height);
         this.graphics.endFill();
         this.dispatchEvent(new Event(Component.DRAW));
      }
   }
}

