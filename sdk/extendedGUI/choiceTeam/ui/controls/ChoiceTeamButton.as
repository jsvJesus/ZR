package ui.controls
{
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.DisplayObjectContainer;
   
   public class ChoiceTeamButton extends SimpleBitmapButton
   {
      public function ChoiceTeamButton(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null, bitmapData:BitmapData = null)
      {
         super(parent,xpos,ypos,"",defaultHandler,new Bitmap(bitmapData,"auto",true),label);
      }
   }
}

