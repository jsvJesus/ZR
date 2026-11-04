package ui.components
{
   import com.dvalimona.components.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import lang.*;
   
   public class PremiumListItem extends ListItem
   {
      protected static const IconSize:uint = 150;
      
      protected static const BorderSize:uint = 4;
      
      private var content:Sprite;
      
      private var box:VBox;
      
      private var icon:Sprite;
      
      private var caption:LabelShadowed;
      
      private var description:TextShadowed;
      
      private var price:LabelShadowed;
      
      private var time:LabelShadowed;
      
      private var back:Sprite;
      
      private var descrLine:HBoxLine;
      
      private var details:ClearButton;
      
      public function PremiumListItem(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, data:Object = null)
      {
         _data = data;
         super(parent,xpos,ypos);
      }
      
      override protected function init() : void
      {
         super.init();
         super.addEventListener(MouseEvent.ROLL_OVER,this.onMouseOver);
         setSize(100,20);
      }
      
      override protected function onMouseOver(event:MouseEvent) : void
      {
         super.addEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
         _mouseOver = true;
         invalidate();
      }
      
      override protected function onMouseOut(event:MouseEvent) : void
      {
         super.removeEventListener(MouseEvent.ROLL_OVER,this.onMouseOut);
         _mouseOver = false;
         invalidate();
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.addChild(this.back = new Sprite());
         this.back.mouseChildren = false;
         this.back.mouseEnabled = false;
         super.addChild(this.content = new Sprite());
         this.content.mouseChildren = false;
         this.content.mouseEnabled = false;
         this.content.addChild(this.icon = new Sprite());
         this.icon.mouseEnabled = false;
         this.icon.mouseChildren = false;
         this.box = new VBox(this.content);
         this.box.spacing = 1;
         this.box.alignment = VBox.LEFT;
         this.box.debug = false;
         this.box.mouseEnabled = false;
         this.box.mouseChildren = true;
         this.caption = new LabelShadowed(this.box);
         this.caption.color = 12700012;
         this.caption.size = 24;
         this.caption.font = Base.fontName;
         this.caption.debug = false;
         this.caption.mouseEnabled = false;
         this.caption.mouseChildren = false;
         this.description = new TextShadowed(this.box);
         this.description.color = 16777215;
         this.description.size = 20;
         this.description.font = Base.lightFontName;
         this.description.leading = -5;
         this.description.debug = false;
         this.description.selectable = false;
         this.description.mouseEnabled = false;
         this.description.mouseChildren = false;
         this.details = new ClearButton();
         this.details.$ = "extendedGUI.PremiumPanel.details";
         this.details.size = 18;
         this.details.height = 32;
         this.details.addEventListener(MouseEvent.CLICK,this.onDetailsClick);
         this.time = new LabelShadowed(this.box);
         this.time.color = 12895428;
         this.time.size = 17;
         this.time.font = Base.lightFontName;
         this.time.paddingTop = 0;
         this.time.debug = false;
         this.time.mouseEnabled = false;
         this.time.mouseChildren = false;
         this.price = new LabelShadowed(this.box);
         this.price.color = 12895428;
         this.price.size = 17;
         this.price.font = Base.lightFontName;
         this.price.paddingTop = -10;
         this.price.debug = false;
         this.price.mouseEnabled = false;
         this.price.mouseChildren = false;
      }
      
      protected function onDetailsClick(event:MouseEvent) : void
      {
         Base.navigator.showDialog(_data.caption,_data.description,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)],500,350);
      }
      
      override public function set data(value:Object) : void
      {
         _data = value;
         invalidate();
      }
      
      override public function get data() : Object
      {
         return _data;
      }
      
      override public function set selected(value:Boolean) : void
      {
         _selected = value;
         invalidate();
      }
      
      override public function get selected() : Boolean
      {
         return _selected;
      }
      
      private function getTimeLabel(time:Number) : String
      {
         var days:uint = 0;
         var hours:uint = 0;
         var minutes:uint = 0;
         var result:String = Locale.getById("extendedGUI.PremiumPanel.time");
         var timeToRemain:uint = time;
         days = timeToRemain / (60 * 60 * 24);
         hours = timeToRemain / (60 * 60);
         minutes = timeToRemain / 60;
         if(days >= 1)
         {
            result += ", " + Locale.getById("extendedGUI.PremiumPanel.days") + ": " + days;
         }
         else if(hours >= 1)
         {
            result += ", " + Locale.getById("extendedGUI.PremiumPanel.hours") + ": " + hours;
         }
         else
         {
            result += ", " + Locale.getById("extendedGUI.PremiumPanel.minutes") + ": " + minutes;
         }
         return result;
      }
      
      override public function draw() : void
      {
         if(_data != null)
         {
            this.caption.text = _data.caption;
            if(_data.description.length < Premium.MAX_DESCRIPTION_LENGTH)
            {
               this.description.text = _data.description;
               if(this.contains(this.details))
               {
                  this.removeChild(this.details);
               }
            }
            else
            {
               this.description.text = _data.description.substr(0,Premium.MAX_DESCRIPTION_LENGTH) + "…";
               if(!this.contains(this.details))
               {
                  this.addChild(this.details);
               }
               if(_mouseOver)
               {
               }
            }
            this.time.text = this.getTimeLabel(_data.time);
            this.price.text = Locale.getById("extendedGUI.PremiumPanel.price") + " " + _data.price;
            while(this.icon.numChildren)
            {
               this.icon.removeChildAt(0);
            }
            this.icon.addChild(_data.icon);
            this.content.visible = true;
            this.back.visible = true;
            this.details.visible = true;
            this.details.x = this.width - this.details.width - 0;
            this.details.y = this.height - this.details.height - 3;
         }
         else
         {
            this.details.visible = false;
            this.content.visible = false;
            this.back.visible = false;
         }
         this.description.width = this.width - IconSize;
         this.back.graphics.clear();
         if(_mouseOver)
         {
            this.back.graphics.beginFill(0,0.75);
         }
         else
         {
            this.back.graphics.beginFill(0,0.5);
         }
         if(_selected)
         {
            this.back.graphics.beginFill(16777215,0.4);
            this.back.graphics.lineStyle(BorderSize,16777215,0.5,true,LineScaleMode.NONE,CapsStyle.SQUARE,JointStyle.MITER);
            this.back.graphics.drawRect(0 + BorderSize / 2,0 + BorderSize / 2,width - BorderSize,height - BorderSize - 1);
         }
         else
         {
            this.back.graphics.drawRect(0,0,this.width,this.height - 1);
         }
         this.back.graphics.endFill();
         this.box.x = IconSize;
         this.box.y = 10;
      }
      
      private function updateLayout() : void
      {
         this.descrLine.width = this.width - IconSize;
         this.description.width = this.descrLine.width - this.details.width;
         this.descrLine.height = this.description.height;
      }
   }
}

