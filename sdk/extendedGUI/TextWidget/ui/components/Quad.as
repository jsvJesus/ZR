package ui.components
{
   import com.dvalimona.components.Component;
   import flash.display.*;
   
   public class Quad extends Component
   {
      private var holder:Sprite;
      
      private var _thickness:int = 2;
      
      protected var color:uint;
      
      public function Quad(param1:DisplayObjectContainer = null, param2:uint = 16777215, param3:Number = 0.5)
      {
         this.color = param2;
         this.alpha = param3;
         super(param1);
      }
      
      public function get thickness() : int
      {
         return this._thickness;
      }
      
      public function set thickness(param1:int) : void
      {
         this._thickness = param1;
         invalidate();
      }
      
      override protected function addChildren() : void
      {
         this.holder = new Sprite();
         this.holder.alpha = alpha;
         this.addChild(this.holder);
      }
      
      override public function draw() : void
      {
         if(this.holder)
         {
            this.holder.graphics.clear();
            this.holder.graphics.beginFill(this.color,1);
            this.holder.graphics.drawRoundRect(0,height / 2 - this.thickness / 2,width,this.thickness,2,2);
            this.holder.graphics.endFill();
         }
      }
   }
}

