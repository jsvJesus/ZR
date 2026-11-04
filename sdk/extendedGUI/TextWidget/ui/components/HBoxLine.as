package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.DisplayObjectContainer;
   
   public class HBoxLine extends Component
   {
      public var left:HBox;
      
      public var right:HBox;
      
      public var drawBack:Boolean = false;
      
      protected var back:ui.components.BlackPanel;
      
      private var _alignment:String = "middle";
      
      public function HBoxLine(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super(param1,param2,param3);
         this.back = new BlackPanel();
         this.addChild(this.back);
         this.left = new HBox(this);
         this.left.spacing = 1;
         this.left.horizontalAlign = HBox.LEFT;
         this.right = new HBox(this);
         this.right.spacing = 1;
         this.right.horizontalAlign = HBox.RIGHT;
      }
      
      public function set alignment(param1:String) : void
      {
         this._alignment = param1;
         this.left.alignment = this._alignment;
         this.right.alignment = this._alignment;
         invalidate();
      }
      
      public function get alignment() : String
      {
         return this._alignment;
      }
      
      override public function draw() : void
      {
         super.draw();
         if(this.drawBack)
         {
            this.back.visible = true;
            this.back.width = _width;
            this.back.height = _height;
         }
         else
         {
            this.back.visible = false;
         }
         this.left.fixedWidth = _width;
         this.left.fixedHeight = _height;
         this.right.fixedWidth = _width;
         this.right.fixedHeight = _height;
      }
   }
}

