package ui
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.events.*;
   
   public class KeyboardMessage extends Component
   {
      private var instructionLabel:LabelShadowed;
      
      private var cancelLabel:LabelShadowed;
      
      private var actionLabel:LabelShadowed;
      
      public var targetButton:*;
      
      public var labelText:String = "hello";
      
      private var cleared:Boolean = false;
      
      public function KeyboardMessage(param1:String, param2:*)
      {
         this.labelText = param1;
         this.targetButton = param2;
         this.addChildren();
         super();
      }
      
      override public function set width(param1:Number) : void
      {
         _width = param1;
         this.draw();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function set height(param1:Number) : void
      {
         _height = param1;
         this.draw();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override protected function init() : void
      {
         setSize(100,100);
      }
      
      override protected function addChildren() : void
      {
         this.actionLabel = new LabelShadowed();
         this.actionLabel.size = 50;
         this.actionLabel.text = this.labelText;
         this.actionLabel.autoSize = true;
         this.actionLabel.align = Label.CENTER;
         this.actionLabel.debug = false;
         this.addChild(this.actionLabel);
         this.instructionLabel = new LabelShadowed();
         this.instructionLabel.size = 16;
         this.instructionLabel.color = 10526608;
         this.instructionLabel.$ = "extendedGUI.Settings.keybindsInstruction1";
         this.instructionLabel.autoSize = false;
         this.instructionLabel.align = Label.LEFT;
         this.instructionLabel.debug = false;
         this.addChild(this.instructionLabel);
         this.cancelLabel = new LabelShadowed();
         this.cancelLabel.size = 16;
         this.cancelLabel.color = 10526608;
         this.cancelLabel.$ = "extendedGUI.Settings.keybindsInstruction2";
         this.cancelLabel.autoSize = false;
         this.cancelLabel.align = Label.RIGHT;
         this.cancelLabel.debug = false;
         this.addChild(this.cancelLabel);
         this.actionLabel.alpha = 0;
         this.instructionLabel.alpha = 0;
         this.cancelLabel.alpha = 0;
         TweenMax.to(this.actionLabel,0.2,{
            "alpha":1,
            "delay":0.1,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         TweenMax.to(this.instructionLabel,0.2,{
            "alpha":1,
            "delay":0.2,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         TweenMax.to(this.cancelLabel,0.2,{
            "alpha":1,
            "delay":0.3,
            "ease":Expo.easeOut,
            "onComplete":null
         });
      }
      
      public function clear() : void
      {
         this.cleared = true;
         this.actionLabel.alpha = 0;
         this.instructionLabel.alpha = 0;
         this.cancelLabel.alpha = 0;
      }
      
      override public function draw() : void
      {
         var _loc1_:uint = Math.max(this.actionLabel.width,200);
         this.actionLabel.x = (width - _loc1_) / 2;
         this.actionLabel.y = (height - this.actionLabel.height) / 2;
         this.actionLabel.width = _loc1_;
         this.instructionLabel.x = this.actionLabel.x;
         this.instructionLabel.width = _loc1_;
         this.instructionLabel.y = this.actionLabel.y - this.instructionLabel.height - 0;
         this.cancelLabel.x = this.instructionLabel.x;
         this.cancelLabel.y = this.actionLabel.y + this.actionLabel.height + 0;
         this.cancelLabel.width = _loc1_;
         this.graphics.clear();
         this.graphics.beginFill(0,0.96);
         this.graphics.drawRect(0,0,width,height);
         if(!this.cleared)
         {
            this.graphics.beginFill(16777215,0.1);
            this.graphics.drawRect(this.instructionLabel.x - 10,this.instructionLabel.y - 10,_loc1_ + 20,this.cancelLabel.y + this.cancelLabel.height - this.instructionLabel.y + 20);
            this.graphics.beginFill(16777215,0.2);
            this.graphics.drawRect(this.instructionLabel.x - 10,this.actionLabel.y - 0,_loc1_ + 20,this.cancelLabel.y - this.actionLabel.y);
         }
         this.graphics.endFill();
      }
   }
}

