package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.Api;
   import events.*;
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   import flash.utils.*;
   import lang.*;
   
   public class EventsPanel extends NewPanelWithIcon
   {
      internal var captionText:String = "";
      
      internal var descrText:String = "";
      
      internal var icon_path:String = "";
      
      internal var end_time:Number = 0;
      
      private var timeElapsed:LabelShadowed;
      
      protected var iconLoader:Loader = new Loader();
      
      public function EventsPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         Api.self.addEventListener(Api.UPDATE_EVENT,this.onUpdateData);
         super(parent,xpos,ypos);
         minMode = false;
         this.visible = false;
         this.mouseEnabled = false;
         this.mouseChildren = false;
      }
      
      public function clearListener() : void
      {
         Api.self.removeEventListener(Api.UPDATE_EVENT,this.onUpdateData);
      }
      
      protected function onUpdateData(arg1:ApiEvent) : void
      {
         this.captionText = arg1.data.answer.caption;
         this.descrText = arg1.data.answer.decription;
         this.icon_path = arg1.data.answer.picture_path;
         this.end_time = arg1.data.answer.end_time;
         title(this.captionText,true);
         text(this.descrText,true);
         this.loadIcon(this.icon_path);
         this.updateView();
         this.onResize();
      }
      
      public function loadIcon(path:String) : *
      {
         var url:URLRequest = new URLRequest(path);
         this.iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.completeIconLoad);
         this.iconLoader.load(url);
      }
      
      protected function updatePositions() : *
      {
      }
      
      protected function completeIconLoad(e:Event) : *
      {
         this.iconLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.completeIconLoad);
         if(this.iconLoader.content != null)
         {
            icon = Bitmap(this.iconLoader.content);
            this.updateView();
            setTimeout(this.updateView,500);
         }
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         caption.labelUpColor = Style.GOLD_OVER;
         caption.labelOverColor = Style.GOLD_OVER;
         this.timeElapsed = new LabelShadowed(standartBox);
         this.timeElapsed.color = 12895428;
         this.timeElapsed.size = 17;
         this.timeElapsed.font = Base.lightFontName;
         this.timeElapsed.paddingTop = 0;
         this.timeElapsed.paddingLeft = 35;
         caption.align = Label.LEFT;
         this.updateView();
         setTimeout(this.updateView,500);
         setTimeout(this.updateView,1000);
         setTimeout(this.updateView,2000);
      }
      
      private function getTimeToEnd(time:Number) : String
      {
         var days:uint = 0;
         var real_days:uint = 0;
         var hours:uint = 0;
         var minutes:uint = 0;
         var result:String = "";
         var timeToRemain:uint = time;
         real_days = timeToRemain / (60 * 60 * 24);
         days = Math.ceil(timeToRemain / (60 * 60 * 24));
         hours = timeToRemain / (60 * 60);
         minutes = timeToRemain / 60;
         if(real_days >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.days") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + days;
         }
         else if(hours >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.hours") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + hours;
         }
         else
         {
            result = Locale.getById("extendedGUI.PremiumPanel.minutes") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + minutes;
         }
         return result;
      }
      
      override public function updateView() : void
      {
         if(!this.contains(standartView))
         {
            this.addChild(standartView);
         }
         for(var i:* = standartIcon.numChildren - 1; i >= 0; i--)
         {
            standartIcon.removeChildAt(i);
         }
         standartBox.x = 148;
         standartBox.y = 40;
         if(_icon != null)
         {
            standartIcon.addChild(_icon);
            _icon.x = (178 - _icon.width) / 2;
         }
         description.width = preferredWidth - standartBox.x - 50 - 0;
         this.timeElapsed.text = this.getTimeToEnd(this.end_time);
         setTimeout(this.tuneHeight,100,true);
         setTimeout(this.tuneHeight,400,true);
         invalidate();
         this.visible = this.captionText.length > 0;
         this.draw();
      }
      
      override protected function tuneHeight(flag:Boolean) : void
      {
         var th:Number = Math.max(standartIcon.y + 168 + 10,this.timeElapsed.y + this.timeElapsed.height + 40);
         description.paddingTop = 18;
         description.size = 20;
         if(minMode)
         {
            th -= 10;
            description.paddingTop = 5;
            description.size = 16;
         }
         TweenMax.to(this,0.3,{
            "height":th,
            "width":preferredWidth,
            "ease":Expo.easeOut
         });
      }
      
      override public function draw() : void
      {
         super.draw();
      }
      
      override public function onResize() : void
      {
         minMode = Base.stage.stageHeight <= 800;
         minMode = Base.stage.stageHeight <= 800;
         this.updateView();
      }
      
      override protected function onMouseOver(e:MouseEvent) : *
      {
      }
      
      override protected function onMouseClick(e:MouseEvent) : *
      {
      }
   }
}

