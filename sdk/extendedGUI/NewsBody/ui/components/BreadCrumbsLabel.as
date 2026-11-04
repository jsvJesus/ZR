package ui.components
{
   import com.dvalimona.components.Component;
   import com.dvalimona.components.Style;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import communication.BreadCrumb;
   import communication.BreadCrumbs;
   import events.BreadCrumbEvent;
   import flash.display.DisplayObjectContainer;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.utils.Dictionary;
   import flash.utils.setTimeout;
   
   public class BreadCrumbsLabel extends Component
   {
      private var textFieldsByCrumbs:Dictionary = new Dictionary();
      
      private var crumbsByTextFields:Dictionary = new Dictionary();
      
      private var itemsIDs:Array = new Array();
      
      private var itemToDelete:TextField;
      
      private var itemToAdd:TextField;
      
      private var activeItem:TextField;
      
      private var activeAlpha:Number = 1;
      
      private var inactiveAlpha:Number = 0.5;
      
      private var activeScale:Number = 1;
      
      private var inactiveScale:Number = 0.5;
      
      private var overAlpha:Number = 1;
      
      private var overColor:uint = 10347511;
      
      private var outColor:uint = 16777215;
      
      public function BreadCrumbsLabel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.ADDED,this.onBreadCrumbsAdded);
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.REMOVED,this.onBreadCrumbsRemoved);
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.CHANGED,this.onBreadCrumbsChanged);
         super(parent,xpos,ypos);
      }
      
      protected function onBreadCrumbsChanged(event:BreadCrumbEvent) : void
      {
      }
      
      protected function onBreadCrumbsRemoved(event:BreadCrumbEvent) : void
      {
         this.removeItem(event.data);
      }
      
      protected function onBreadCrumbsAdded(event:BreadCrumbEvent) : void
      {
         this.addItem(event.data);
      }
      
      private function createItem() : TextField
      {
         var tf:TextField = new TextField();
         tf.defaultTextFormat = new TextFormat(Style.boldFontName,30,16777215);
         tf.autoSize = TextFieldAutoSize.LEFT;
         tf.borderColor = 16777215;
         tf.border = false;
         tf.embedFonts = false;
         this.addChild(tf);
         tf.addEventListener(MouseEvent.ROLL_OVER,this.onOver);
         tf.addEventListener(MouseEvent.ROLL_OUT,this.onOut);
         tf.addEventListener(MouseEvent.CLICK,this.onClick);
         return tf;
      }
      
      protected function onClick(event:MouseEvent) : void
      {
         this.goToItem(event.target as TextField);
      }
      
      protected function goToItem(item:TextField) : void
      {
      }
      
      protected function onOut(event:MouseEvent) : void
      {
         var item:TextField = event.target as TextField;
         TweenMax.to(item,0.3,{
            "alpha":(item == this.activeItem ? this.activeAlpha : this.inactiveAlpha),
            "ease":Expo.easeOut
         });
      }
      
      protected function onOver(event:MouseEvent) : void
      {
         var item:TextField = event.target as TextField;
         TweenMax.to(item,0.5,{
            "alpha":this.overAlpha,
            "ease":Expo.easeOut
         });
      }
      
      private function addItem(data:BreadCrumb) : void
      {
         var newItem:TextField = this.createItem();
         this.textFieldsByCrumbs[data] = newItem;
         this.crumbsByTextFields[newItem] = data;
         newItem.text = data.screen.label;
         this.itemToAdd = newItem;
         this.updateLabelsView();
         invalidate();
      }
      
      private function isExist(data:BreadCrumb) : Boolean
      {
         var result:Boolean = false;
         for(var i:uint = 0; i < this.numChildren; i++)
         {
            if(this.textFieldsByCrumbs[data] != null)
            {
               result = true;
            }
         }
         return result;
      }
      
      private function removeItem(data:BreadCrumb) : void
      {
         var item:TextField = this.textFieldsByCrumbs[data] as TextField;
         if(item != null)
         {
            this.itemToDelete = item;
            setTimeout(this.killItem,1000,item);
            this.updateLabelsView();
            invalidate();
         }
      }
      
      private function killItem(item:TextField) : void
      {
         delete this.crumbsByTextFields[item];
         this.removeChild(item);
      }
      
      private function updateLabelsView() : void
      {
         var item:TextField = null;
         var i:uint = 0;
         var inactives:Array = [];
         for(i = 0; i < this.numChildren; i++)
         {
            item = this.getChildAt(i) as TextField;
            if(item == this.itemToDelete)
            {
               this.activeItem = inactives.pop();
            }
            else if(item == this.itemToAdd)
            {
               this.activeItem = item;
            }
            else
            {
               inactives.push(item);
            }
         }
         for(i = 0; i < inactives.length; i++)
         {
            item = inactives[i] as TextField;
            TweenMax.to(item,1,{
               "alpha":0.5,
               "y":14,
               "scaleX":this.inactiveScale,
               "scaleY":this.inactiveScale,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
         }
         if(this.activeItem != null)
         {
            if(this.activeItem == this.itemToAdd)
            {
               this.activeItem.alpha = 0;
            }
            TweenMax.to(this.activeItem,1,{
               "alpha":1,
               "y":0,
               "z":0,
               "scaleX":this.activeScale,
               "scaleY":this.activeScale,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
         }
         if(this.itemToDelete != null)
         {
            TweenMax.to(this.itemToDelete,0.5,{
               "alpha":0,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
         }
         this.itemToDelete = null;
         this.itemToAdd = null;
      }
      
      private function alignItems() : void
      {
         var lastWidth:Number = NaN;
         var item:TextField = null;
         var lastPos:Number = 0;
         for(var i:uint = 0; i < this.numChildren; i++)
         {
            item = this.getChildAt(i) as TextField;
            item.x = lastPos;
            lastPos += item.width + 5;
         }
      }
      
      override public function draw() : void
      {
         this.alignItems();
      }
   }
}

