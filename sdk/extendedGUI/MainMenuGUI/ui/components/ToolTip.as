package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   
   public class ToolTip extends Component
   {
      protected var back:ui.components.BlackPanel;
      
      protected var text:TextShadowed;
      
      public function ToolTip(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         this.width = 400;
         this.height = 60;
         super(parent,xpos,ypos);
      }
      
      protected function onBoosterUpdated(event:Event) : void
      {
         updateView();
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.back = new BlackPanel();
         this.back.alpha = 0.9;
         this.addChild(this.back);
         this.text = new TextShadowed(this);
         this.text.editable = false;
         this.text.selectable = false;
         this.text.autoHeight = true;
         this.text.size = 18;
         this.text.color = 11776934;
         this.text.$ = "extendedGUI.RootWindow.premiumHint1";
         this.text.debug = false;
         this.text.y = 15;
         this.text.x = 15;
         this.text.width = 370;
         this.text.draw();
      }
      
      public function showToolTip(sender:Object, msg:String) : *
      {
         (sender as Component).addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         (sender as Component).addEventListener(MouseEvent.MOUSE_OUT,this.onMouseOut);
         this.text.text = msg;
         this.update();
         this.visible = true;
      }
      
      protected function onMouseMove(event:MouseEvent) : *
      {
         this.x = stage.mouseX + 5;
         this.y = stage.mouseY;
      }
      
      protected function onMouseOut(event:MouseEvent) : *
      {
         event.target.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         event.target.removeEventListener(MouseEvent.MOUSE_OUT,this.onMouseOut);
         this.visible = false;
      }
      
      internal function update() : void
      {
         this.height = this.text.height + 30;
         this.back.width = width;
         this.back.height = height;
         this.x = stage.mouseX + 5;
         this.y = stage.mouseY;
      }
      
      override public function draw() : void
      {
         this.update();
         super.draw();
      }
   }
}

